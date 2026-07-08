using BusinessEntity = DukkanPilot.Core.Entities.Business;
using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Admin.Models;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Route("Admin/Dashboard")]
public class DashboardController : AdminBaseController
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notifications;
    private readonly ISalesRequestService _salesRequests;
    private readonly CustomerOnboardingHelper _onboardingHelper;
    private readonly CustomerSuccessHealthHelper _successHelper;
    private readonly IBillingOperationsService _billing;

    public DashboardController(
        AppDbContext context,
        INotificationService notifications,
        ISalesRequestService salesRequests,
        CustomerOnboardingHelper onboardingHelper,
        CustomerSuccessHealthHelper successHelper,
        IBillingOperationsService billing)
    {
        _context = context;
        _notifications = notifications;
        _salesRequests = salesRequests;
        _onboardingHelper = onboardingHelper;
        _successHelper = successHelper;
        _billing = billing;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "dashboard";

        await _notifications.GenerateSmartAdminAlertsAsync();

        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);
        var last7Start = todayStart.AddDays(-6);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var businesses = await _context.Businesses
            .AsNoTracking()
            .Include(b => b.Subscriptions)
                .ThenInclude(s => s.SubscriptionPlan)
            .ToListAsync();

        var orders = await _context.Orders.AsNoTracking().ToListAsync();
        var revenueOrders = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();

        var productCounts = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .GroupBy(p => p.BusinessId)
            .Select(g => new { BusinessId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BusinessId, x => x.Count);

        var categoryCounts = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .GroupBy(c => c.BusinessId)
            .Select(g => new { BusinessId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BusinessId, x => x.Count);

        var ownerEmails = await _context.UserBusinessRoles
            .AsNoTracking()
            .Where(r => r.Role == BusinessRole.Owner && r.IsActive && r.AppUser.IsActive)
            .Select(r => new { r.BusinessId, r.AppUser.Email })
            .ToListAsync();

        var ownerEmailByBusiness = ownerEmails
            .GroupBy(x => x.BusinessId)
            .ToDictionary(g => g.Key, g => g.First().Email);

        var orderStatsByBusiness = orders
            .GroupBy(o => o.BusinessId)
            .ToDictionary(
                g => g.Key,
                g => new BusinessOrderStats
                {
                    Count = g.Count(),
                    Revenue = g.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount),
                    LastOrderAt = g.Max(o => (DateTime?)o.CreatedAt)
                });

        var businessActivities = businesses.Select(b =>
        {
            var latestSub = AdminSaasQueryHelper.GetLatestSubscription(b.Subscriptions);
            orderStatsByBusiness.TryGetValue(b.Id, out var orderStats);
            productCounts.TryGetValue(b.Id, out var activeProducts);

            return new AdminBusinessActivityViewModel
            {
                BusinessId = b.Id,
                BusinessName = b.Name,
                Slug = b.Slug,
                OrderCount = orderStats?.Count ?? 0,
                Revenue = orderStats?.Revenue ?? 0m,
                ActiveProductsCount = activeProducts,
                LastOrderAt = orderStats?.LastOrderAt,
                CurrentPlanName = latestSub?.SubscriptionPlan?.Name ?? "-",
                SubscriptionStatus = latestSub is not null
                    ? AdminSaasQueryHelper.GetStatusLabel(latestSub.Status)
                    : "-",
                SubscriptionStatusBadgeClass = latestSub is not null
                    ? AdminSaasQueryHelper.GetStatusBadgeClass(latestSub.Status)
                    : "bg-secondary",
                CreatedAt = b.CreatedAt,
                OwnerEmail = ownerEmailByBusiness.GetValueOrDefault(b.Id)
            };
        }).ToList();

        var model = new AdminDashboardViewModel
        {
            Platform = new AdminPlatformKpiViewModel
            {
                TotalBusinesses = businesses.Count,
                ActiveBusinesses = businesses.Count(b => b.IsActive),
                PassiveBusinesses = businesses.Count(b => !b.IsActive),
                TotalUsers = await _context.AppUsers.CountAsync(u => u.IsActive),
                BusinessOwnersCount = await _context.AppUsers.CountAsync(u => u.IsActive && u.Role == UserRole.BusinessOwner),
                StaffUsersCount = await _context.AppUsers.CountAsync(u => u.IsActive && u.Role == UserRole.Staff),
                TotalOrders = orders.Count,
                TotalRevenue = revenueOrders.Sum(o => o.TotalAmount),
                TodayOrders = orders.Count(o => o.CreatedAt >= todayStart && o.CreatedAt < todayEnd),
                TodayRevenue = revenueOrders
                    .Where(o => o.CreatedAt >= todayStart && o.CreatedAt < todayEnd)
                    .Sum(o => o.TotalAmount),
                Last7DaysOrders = orders.Count(o => o.CreatedAt >= last7Start),
                Last7DaysRevenue = revenueOrders.Where(o => o.CreatedAt >= last7Start).Sum(o => o.TotalAmount),
                ThisMonthOrders = orders.Count(o => o.CreatedAt >= monthStart),
                ThisMonthRevenue = revenueOrders.Where(o => o.CreatedAt >= monthStart).Sum(o => o.TotalAmount)
            },
            Subscriptions = BuildSubscriptionKpis(businesses, now),
            PlanDistribution = BuildPlanDistribution(businesses, now),
            TopActiveBusinesses = businessActivities
                .OrderByDescending(b => b.Revenue)
                .ThenByDescending(b => b.OrderCount)
                .Take(10)
                .ToList(),
            RiskyBusinesses = BuildRiskyBusinesses(businesses, orderStatsByBusiness, productCounts, categoryCounts, now)
                .Take(10)
                .ToList(),
            RecentBusinesses = businessActivities
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .ToList(),
            CriticalNotifications = await BuildCriticalNotificationsAsync()
        };

        var salesSummary = await _salesRequests.GetAdminSummaryAsync();
        model.NewSalesRequestCount = salesSummary.NewCount;
        model.OpenSalesRequestCount = salesSummary.OpenCount;

        var onboardingSnaps = await _onboardingHelper.BuildForBusinessesAsync(businesses.Select(b => b.Id));
        model.OnboardingAtRiskCount = onboardingSnaps.Count(s => s.IsAtRisk || s.Score < 40);
        model.OnboardingLiveCount = onboardingSnaps.Count(s => s.IsLive);

        var successSnaps = await _successHelper.BuildForBusinessesAsync(businesses.Select(b => b.Id));
        model.CustomerSuccessAtRiskCount = successSnaps.Count(s => s.IsAtRisk);
        model.CustomerSuccessHealthyCount = successSnaps.Count(s => s.IsHealthyOrBetter);

        var billingSummary = await _billing.GetAdminBillingSummaryAsync();
        model.BillingOpenAmount = billingSummary.OpenAmount;
        model.BillingOverdueCount = billingSummary.OverdueCount;
        model.BillingPaidThisMonth = billingSummary.PaidThisMonth;

        return View(model);
    }

    private async Task<List<NotificationRowViewModel>> BuildCriticalNotificationsAsync()
    {
        return await _context.Notifications.AsNoTracking()
            .Where(n => n.Area == "Admin" && !n.IsRead && (n.Severity == "Critical" || n.Severity == "Warning"))
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(5)
            .Select(n => new NotificationRowViewModel
            {
                Id = n.Id,
                CreatedAtUtc = n.CreatedAtUtc,
                Area = n.Area,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                ActionUrl = n.ActionUrl,
                Severity = n.Severity,
                IsRead = n.IsRead,
                BusinessId = n.BusinessId
            })
            .ToListAsync();
    }

    private static AdminSubscriptionKpiViewModel BuildSubscriptionKpis(List<BusinessEntity> businesses, DateTime now)
    {
        var kpis = new AdminSubscriptionKpiViewModel();

        foreach (var business in businesses)
        {
            var latestSub = AdminSaasQueryHelper.GetLatestSubscription(business.Subscriptions);
            if (latestSub is null)
            {
                kpis.BusinessesWithoutSubscription++;
                continue;
            }

            if (AdminSaasQueryHelper.IsExpiringSoon(latestSub, now))
            {
                kpis.ExpiringSoonSubscriptions++;
            }

            if (AdminSaasQueryHelper.IsExpiredSubscription(latestSub, now))
            {
                kpis.ExpiredSubscriptions++;
                continue;
            }

            switch (latestSub.Status)
            {
                case SubscriptionStatus.Trial when AdminSaasQueryHelper.IsSubscriptionValid(latestSub, now):
                    kpis.TrialSubscriptions++;
                    break;
                case SubscriptionStatus.Active when AdminSaasQueryHelper.IsSubscriptionValid(latestSub, now):
                    kpis.ActiveSubscriptions++;
                    break;
                case SubscriptionStatus.Cancelled:
                    kpis.CancelledSubscriptions++;
                    break;
                case SubscriptionStatus.Expired:
                    kpis.ExpiredSubscriptions++;
                    break;
            }
        }

        return kpis;
    }

    private static List<AdminPlanDistributionViewModel> BuildPlanDistribution(List<BusinessEntity> businesses, DateTime now)
    {
        var planIds = businesses
            .SelectMany(b => b.Subscriptions)
            .Select(s => s.SubscriptionPlan)
            .Where(p => p is not null)
            .Select(p => p!)
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToList();

        return planIds.Select(plan =>
        {
            var latestPerBusiness = businesses
                .Select(b => AdminSaasQueryHelper.GetLatestSubscription(b.Subscriptions))
                .Where(s => s?.SubscriptionPlanId == plan.Id)
                .ToList();

            var activeCount = latestPerBusiness.Count(s =>
                s is not null && AdminSaasQueryHelper.IsSubscriptionValid(s, now));

            return new AdminPlanDistributionViewModel
            {
                PlanId = plan.Id,
                PlanName = plan.Name,
                BusinessCount = latestPerBusiness.Count(s => s is not null),
                ActiveSubscriptionCount = activeCount,
                MonthlyPotentialRevenue = activeCount * plan.Price
            };
        })
        .OrderByDescending(p => p.BusinessCount)
        .ToList();
    }

    private static List<AdminRiskyBusinessViewModel> BuildRiskyBusinesses(
        List<BusinessEntity> businesses,
        Dictionary<int, BusinessOrderStats> orderStatsByBusiness,
        Dictionary<int, int> productCounts,
        Dictionary<int, int> categoryCounts,
        DateTime now)
    {
        var risks = new List<AdminRiskyBusinessViewModel>();
        var last30Days = now.AddDays(-30);

        foreach (var business in businesses.Where(b => b.IsActive))
        {
            var latestSub = AdminSaasQueryHelper.GetLatestSubscription(business.Subscriptions);
            orderStatsByBusiness.TryGetValue(business.Id, out var orderStats);
            productCounts.TryGetValue(business.Id, out var activeProducts);
            categoryCounts.TryGetValue(business.Id, out var activeCategories);

            var revenue = orderStats?.Revenue ?? 0m;
            var lastOrderAt = orderStats?.LastOrderAt;
            var orderCount = orderStats?.Count ?? 0;

            if (AdminSaasQueryHelper.IsExpiredSubscription(latestSub, now))
            {
                risks.Add(CreateRisk(business, "Abonelik süresi dolmuş", "bg-danger", revenue, lastOrderAt, latestSub?.EndDate));
            }
            else if (latestSub is not null && AdminSaasQueryHelper.IsExpiringSoon(latestSub, now))
            {
                risks.Add(CreateRisk(business, "7 gün içinde bitecek", "bg-warning text-dark", revenue, lastOrderAt, latestSub.EndDate));
            }

            if (activeProducts == 0)
            {
                risks.Add(CreateRisk(business, "Aktif ürün yok", "bg-secondary", revenue, lastOrderAt));
            }

            if (activeCategories == 0)
            {
                risks.Add(CreateRisk(business, "QR menü eksik", "bg-info text-dark", revenue, lastOrderAt));
            }

            if (!lastOrderAt.HasValue || lastOrderAt.Value < last30Days)
            {
                risks.Add(CreateRisk(business, "Son 30 gün sipariş yok", "bg-warning text-dark", revenue, lastOrderAt));
            }
        }

        return risks
            .GroupBy(r => new { r.BusinessId, r.RiskReason })
            .Select(g => g.First())
            .OrderByDescending(r => r.RiskBadgeClass.Contains("danger", StringComparison.Ordinal))
            .ThenBy(r => r.LastOrderAt)
            .ToList();
    }

    private static AdminRiskyBusinessViewModel CreateRisk(
        BusinessEntity business,
        string reason,
        string badgeClass,
        decimal revenue,
        DateTime? lastOrderAt,
        DateTime? subscriptionEndDate = null) => new()
    {
        BusinessId = business.Id,
        BusinessName = business.Name,
        Slug = business.Slug,
        RiskReason = reason,
        RiskBadgeClass = badgeClass,
        TotalRevenue = revenue,
        LastOrderAt = lastOrderAt,
        SubscriptionEndDate = subscriptionEndDate
    };

    private sealed class BusinessOrderStats
    {
        public int Count { get; init; }

        public decimal Revenue { get; init; }

        public DateTime? LastOrderAt { get; init; }
    }
}
