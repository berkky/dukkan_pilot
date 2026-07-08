using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Admin.Models;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Models.Onboarding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Route("Admin/SalesCenter")]
public class SalesCenterController : AdminBaseController
{
    private readonly AppDbContext _context;
    private readonly CustomerOnboardingHelper _onboardingHelper;

    public SalesCenterController(AppDbContext context, CustomerOnboardingHelper onboardingHelper)
    {
        _context = context;
        _onboardingHelper = onboardingHelper;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "sales-center";

        var now = DateTime.UtcNow;
        var businesses = await _context.Businesses.AsNoTracking()
            .Include(b => b.Setting)
            .Include(b => b.Subscriptions)
            .ToListAsync();

        var productCounts = await _context.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .GroupBy(p => p.BusinessId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var categoryCounts = await _context.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .GroupBy(c => c.BusinessId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var campaignCounts = await _context.Campaigns.AsNoTracking()
            .Where(c => c.IsActive)
            .GroupBy(c => c.BusinessId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var orderCounts = await _context.Orders.AsNoTracking()
            .GroupBy(o => o.BusinessId)
            .Select(g => new { g.Key, Count = g.Count(), Last = g.Max(o => o.CreatedAt) })
            .ToDictionaryAsync(x => x.Key, x => x);

        var notificationCounts = await _context.Notifications.AsNoTracking()
            .Where(n => n.BusinessId != null)
            .GroupBy(n => n.BusinessId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var auditCounts = await _context.AuditLogs.AsNoTracking()
            .Where(a => a.BusinessId != null)
            .GroupBy(a => a.BusinessId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var rows = new List<SalesCenterBusinessRowViewModel>();
        var trial = 0;
        var expiring = 0;

        foreach (var business in businesses)
        {
            var latestSub = AdminSaasQueryHelper.GetLatestSubscription(business.Subscriptions);
            if (latestSub is not null
                && AdminSaasQueryHelper.IsSubscriptionValid(latestSub, now)
                && latestSub.Status == Core.Enums.SubscriptionStatus.Trial)
            {
                trial++;
            }

            if (latestSub is not null && AdminSaasQueryHelper.IsExpiringSoon(latestSub, now))
            {
                expiring++;
            }

            var products = productCounts.GetValueOrDefault(business.Id);
            var categories = categoryCounts.GetValueOrDefault(business.Id);
            var campaigns = campaignCounts.GetValueOrDefault(business.Id);
            var orderInfo = orderCounts.GetValueOrDefault(business.Id);
            var orderCount = orderInfo?.Count ?? 0;
            var lastOrder = orderInfo?.Last;
            var hasWhatsApp = !string.IsNullOrWhiteSpace(business.Setting?.WhatsAppNumber)
                || !string.IsNullOrWhiteSpace(business.Phone);

            var health = AdminBusinessHealthHelper.Evaluate(
                AdminBusinessHealthHelper.CreateInput(business, latestSub, categories, products, lastOrder, now));

            var demoReady = business.IsActive
                && hasWhatsApp
                && categories > 0
                && products > 0
                && !string.IsNullOrWhiteSpace(business.Slug);

            string? attention = null;
            if (!business.IsActive)
            {
                attention = "Pasif işletme";
            }
            else if (products == 0)
            {
                attention = "Aktif ürün yok";
            }
            else if (!hasWhatsApp)
            {
                attention = "WhatsApp/telefon eksik";
            }
            else if (orderCount == 0)
            {
                attention = "Sipariş yok";
            }
            else if (latestSub is null || AdminSaasQueryHelper.IsExpiredSubscription(latestSub, now))
            {
                attention = "Abonelik riski";
            }
            else if (AdminSaasQueryHelper.IsExpiringSoon(latestSub, now))
            {
                attention = "Abonelik yakında bitiyor";
            }

            rows.Add(new SalesCenterBusinessRowViewModel
            {
                BusinessId = business.Id,
                BusinessName = business.Name,
                Slug = business.Slug,
                PublicMenuUrl = $"/m/{business.Slug}",
                HealthScore = health.Score,
                HealthLabel = health.Label,
                HealthBadgeClass = health.BadgeClass,
                OrderCount = orderCount,
                ActiveProductCount = products,
                CampaignCount = campaigns,
                NotificationCount = notificationCounts.GetValueOrDefault(business.Id),
                AuditLogCount = auditCounts.GetValueOrDefault(business.Id),
                IsDemoReady = demoReady,
                AttentionReason = attention
            });
        }

        var onboardingSnaps = await _onboardingHelper.BuildForBusinessesAsync(businesses.Select(b => b.Id));
        var onboardingById = onboardingSnaps.ToDictionary(s => s.BusinessId);
        var onboardingReady = onboardingSnaps
            .Where(s => s.Status is OnboardingStatus.ReadyToLaunch or OnboardingStatus.Live)
            .OrderByDescending(s => s.Score)
            .Take(15)
            .Select(s => new SalesCenterBusinessRowViewModel
            {
                BusinessId = s.BusinessId,
                BusinessName = s.BusinessName,
                Slug = s.BusinessSlug,
                PublicMenuUrl = s.PublicMenuUrl,
                HealthScore = s.Score,
                HealthLabel = s.StatusLabel,
                HealthBadgeClass = s.StatusBadgeClass,
                OrderCount = s.OrderCount,
                ActiveProductCount = s.ActiveProductCount,
                CampaignCount = s.CampaignCount,
                IsDemoReady = true
            })
            .ToList();

        var wonRequests = await _context.SalesRequests.AsNoTracking()
            .Where(r => r.Status == "Won")
            .OrderByDescending(r => r.ClosedAtUtc ?? r.UpdatedAtUtc ?? r.CreatedAtUtc)
            .Take(12)
            .ToListAsync();

        var wonHandoffs = wonRequests.Select(r =>
        {
            CustomerOnboardingSnapshot? snap = null;
            if (r.BusinessId is int bid)
            {
                onboardingById.TryGetValue(bid, out snap);
            }

            return new SalesCenterWonHandoffViewModel
            {
                SalesRequestId = r.Id,
                ContactName = r.ContactName ?? "-",
                BusinessName = r.BusinessName,
                BusinessId = r.BusinessId,
                OnboardingScore = snap?.Score,
                OnboardingStatusLabel = snap?.StatusLabel,
                OnboardingBadgeClass = snap?.StatusBadgeClass
            };
        }).ToList();

        var model = new SalesCenterViewModel
        {
            TotalBusinesses = businesses.Count,
            ActiveBusinesses = businesses.Count(b => b.IsActive),
            TrialBusinesses = trial,
            ExpiringSoonBusinesses = expiring,
            DemoReadyBusinesses = rows.Count(r => r.IsDemoReady),
            OnboardingReadyBusinesses = onboardingReady.Count,
            DemoReadyList = rows.Where(r => r.IsDemoReady)
                .OrderByDescending(r => r.HealthScore)
                .ThenByDescending(r => r.OrderCount)
                .Take(15)
                .ToList(),
            OnboardingReadyList = onboardingReady,
            NeedsAttentionList = rows.Where(r => !string.IsNullOrWhiteSpace(r.AttentionReason))
                .OrderBy(r => r.HealthScore)
                .ThenBy(r => r.BusinessName)
                .Take(15)
                .ToList(),
            WonHandoffs = wonHandoffs
        };

        return View(model);
    }
}
