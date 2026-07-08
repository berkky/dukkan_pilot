using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

public class DashboardController : BusinessBaseController
{
    private readonly AppDbContext _context;
    private readonly BusinessSubscriptionStatusHelper _subscriptionStatusHelper;
    private readonly BusinessPlanLimitHelper _planLimitHelper;
    private readonly GoLiveHelper _goLiveHelper;
    private readonly CustomerOnboardingHelper _onboardingHelper;
    private readonly INotificationService _notifications;

    public DashboardController(
        AppDbContext context,
        BusinessSubscriptionStatusHelper subscriptionStatusHelper,
        BusinessPlanLimitHelper planLimitHelper,
        GoLiveHelper goLiveHelper,
        CustomerOnboardingHelper onboardingHelper,
        INotificationService notifications)
    {
        _context = context;
        _subscriptionStatusHelper = subscriptionStatusHelper;
        _planLimitHelper = planLimitHelper;
        _goLiveHelper = goLiveHelper;
        _onboardingHelper = onboardingHelper;
        _notifications = notifications;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "dashboard";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        await _notifications.GenerateSmartBusinessAlertsAsync(businessId);

        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business is null)
        {
            return NotFound();
        }

        var now = DateTime.UtcNow;
        var campaigns = _context.Campaigns.AsNoTracking().Where(c => c.BusinessId == businessId);

        var nearestEnding = await campaigns
            .Where(c => c.IsActive && c.EndDate != null && c.EndDate >= now)
            .OrderBy(c => c.EndDate)
            .Select(c => new { c.Title, c.EndDate })
            .FirstOrDefaultAsync();

        var (todayStartUtc, todayEndUtc) = OrderQueryHelper.GetTodayUtcRange();
        var last7DaysStartUtc = OrderQueryHelper.GetWeekStartUtc();
        var (monthStartUtc, monthEndUtc) = OrderQueryHelper.GetCurrentMonthUtcRange();
        var ordersQuery = _context.Orders.AsNoTracking().Where(o => o.BusinessId == businessId);

        var latestOrderSnapshot = await ordersQuery
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new { o.Id, o.CreatedAt })
            .FirstOrDefaultAsync();

        var hasOrders = await ordersQuery.AnyAsync();

        var topProducts = await _context.OrderItems
            .AsNoTracking()
            .Where(i => i.Order.BusinessId == businessId && i.Order.Status != OrderStatus.Cancelled)
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new ProductSalesRowViewModel
            {
                ProductName = g.Key.ProductName,
                QuantitySold = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.UnitPrice * i.Quantity)
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(5)
            .ToListAsync();

        var monthCampaignOrders = ordersQuery
            .Where(o => o.CreatedAt >= monthStartUtc
                && o.CreatedAt < monthEndUtc
                && o.Status != OrderStatus.Cancelled
                && o.DiscountAmount > 0);

        var topCampaignThisMonth = await monthCampaignOrders
            .GroupBy(o => o.AppliedCampaignName ?? "Bilinmeyen Kampanya")
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .FirstOrDefaultAsync();

        var model = new BusinessDashboardViewModel
        {
            BusinessName = business.Name,
            BusinessSlug = business.Slug,
            TotalCategoryCount = await _context.Categories.CountAsync(c => c.BusinessId == businessId),
            ActiveCategoryCount = await _context.Categories.CountAsync(c => c.BusinessId == businessId && c.IsActive),
            TotalProductCount = await _context.Products.CountAsync(p => p.BusinessId == businessId),
            ActiveProductCount = await _context.Products.CountAsync(p => p.BusinessId == businessId && p.IsActive),
            TotalOrderCount = await ordersQuery.CountAsync(),
            TotalCustomerCount = await _context.Customers.CountAsync(c => c.BusinessId == businessId),
            ActiveCustomerCount = await _context.Customers.CountAsync(c => c.BusinessId == businessId && c.IsActive),
            OrderSummary = new DashboardOrderSummaryViewModel
            {
                TodayOrderCount = await ordersQuery.CountAsync(o =>
                    o.CreatedAt >= todayStartUtc && o.CreatedAt < todayEndUtc),
                PendingCount = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Pending),
                PreparingCount = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Preparing),
                TodayRevenue = await ordersQuery
                    .Where(o => o.CreatedAt >= todayStartUtc && o.CreatedAt < todayEndUtc)
                    .SumAsync(o => o.TotalAmount),
                Last7DaysRevenue = await ordersQuery
                    .Where(o => o.CreatedAt >= last7DaysStartUtc)
                    .SumAsync(o => o.TotalAmount),
                MonthlyRevenue = await ordersQuery
                    .Where(o => o.CreatedAt >= monthStartUtc && o.CreatedAt < monthEndUtc)
                    .SumAsync(o => o.TotalAmount),
                AverageOrderAmount = hasOrders
                    ? await ordersQuery.AverageAsync(o => o.TotalAmount)
                    : 0,
                LatestOrderId = latestOrderSnapshot?.Id,
                LatestOrderCreatedAt = latestOrderSnapshot?.CreatedAt
            },
            StatusDistribution = new DashboardStatusDistributionViewModel
            {
                PendingCount = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Pending),
                PreparingCount = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Preparing),
                CompletedCount = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Completed),
                CancelledCount = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Cancelled)
            },
            TopProducts = topProducts,
            RecentOrders = await ordersQuery
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new RecentOrderViewModel
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.CustomerName,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync(),
            LoyaltySummary = new LoyaltySummaryViewModel
            {
                TotalActiveCustomerPoints = await _context.Customers
                    .Where(c => c.BusinessId == businessId && c.IsActive)
                    .SumAsync(c => c.TotalPoints),
                LastTransactionDate = await _context.LoyaltyTransactions
                    .Where(t => t.BusinessId == businessId)
                    .MaxAsync(t => (DateTime?)t.CreatedAt),
                HasActiveLoyaltyRule = await _context.LoyaltyRules
                    .AnyAsync(r => r.BusinessId == businessId && r.IsActive)
            },
            CampaignSummary = new CampaignDashboardSummaryViewModel
            {
                TotalCampaignCount = await campaigns.CountAsync(),
                ActiveCampaignCount = await campaigns.CountAsync(c => c.IsActive),
                PublishedCampaignCount = await campaigns.CountAsync(c =>
                    c.IsActive &&
                    c.StartDate <= now &&
                    (c.EndDate == null || c.EndDate >= now)),
                NearestEndingCampaignTitle = nearestEnding?.Title,
                NearestEndingCampaignEndDate = nearestEnding?.EndDate,
                MonthCampaignOrderCount = await monthCampaignOrders.CountAsync(),
                MonthTotalDiscount = await monthCampaignOrders.SumAsync(o => (decimal?)o.DiscountAmount) ?? 0m,
                TopCampaignNameThisMonth = topCampaignThisMonth?.Name
            },
            Subscription = await _subscriptionStatusHelper.GetStatusAsync(businessId),
            PlanUsage = await _planLimitHelper.GetUsageAsync(businessId),
            GoLiveStatus = await _goLiveHelper.BuildDashboardCardAsync(businessId),
            OnboardingStatus = await _onboardingHelper.BuildDashboardCardAsync(businessId),
            Notifications = await BuildNotificationCardAsync(businessId),
            IsBusinessOwner = User.IsInRole(nameof(UserRole.BusinessOwner))
        };

        return View(model);
    }

    private async Task<DashboardNotificationCardViewModel> BuildNotificationCardAsync(int businessId)
    {
        var query = _context.Notifications.AsNoTracking()
            .Where(n => n.BusinessId == businessId);

        var unread = query.Where(n => !n.IsRead);
        var recent = await query
            .OrderBy(n => n.IsRead)
            .ThenByDescending(n => n.CreatedAtUtc)
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

        return new DashboardNotificationCardViewModel
        {
            UnreadCount = await unread.CountAsync(),
            CriticalCount = await unread.CountAsync(n => n.Severity == "Critical"),
            RecentItems = recent
        };
    }
}
