using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Admin.Models;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Models.Success;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Route("Admin/CustomerSuccess")]
public class CustomerSuccessController : AdminBaseController
{
    private readonly AppDbContext _context;
    private readonly CustomerSuccessHealthHelper _successHelper;

    public CustomerSuccessController(AppDbContext context, CustomerSuccessHealthHelper successHelper)
    {
        _context = context;
        _successHelper = successHelper;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? status = null,
        string? churnRisk = null,
        string? expansion = null,
        string? subscriptionStatus = null,
        bool? noOrdersLast30Days = null,
        bool? hasUpgradeRequest = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["ActiveMenu"] = "customer-success";

        var businessIds = await _context.Businesses.AsNoTracking()
            .Where(b => b.IsActive)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var snapshots = await _successHelper.BuildForBusinessesAsync(businessIds, cancellationToken);

        var openUpgradeBusinessIds = await _context.SalesRequests.AsNoTracking()
            .Where(r => r.BusinessId != null
                && r.RequestType == "UpgradeRequest"
                && (r.Status == "New" || r.Status == "Contacted" || r.Status == "Qualified" || r.Status == "WaitingCustomer"))
            .Select(r => r.BusinessId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var upgradeSet = openUpgradeBusinessIds.ToHashSet();

        var wonLeadBusinessIds = await _context.SalesRequests.AsNoTracking()
            .Where(r => r.BusinessId != null && r.Status == "Won")
            .Select(r => r.BusinessId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var wonLeadSet = wonLeadBusinessIds.ToHashSet();

        var rows = snapshots.Select(s => new AdminCustomerSuccessRowViewModel
        {
            BusinessId = s.BusinessId,
            BusinessName = s.BusinessName,
            Slug = s.BusinessSlug,
            PublicMenuUrl = s.PublicMenuUrl,
            PlanName = s.Subscription.PlanName,
            SubscriptionStatusText = s.Subscription.StatusText,
            SubscriptionStatusBadgeClass = s.Subscription.StatusCssClass,
            Score = s.Score,
            StatusLabel = s.StatusLabel,
            StatusBadgeClass = s.StatusBadgeClass,
            ChurnRiskLabel = s.ChurnRiskLabel,
            ChurnRiskBadgeClass = s.ChurnRiskBadgeClass,
            ExpansionLabel = s.ExpansionPotentialLabel,
            ExpansionBadgeClass = s.ExpansionPotentialBadgeClass,
            LastOrderAtUtc = s.LastOrderAtUtc,
            LastActivityAtUtc = s.LastActivityAtUtc,
            OrdersLast30Days = s.Kpis.OrdersLast30Days,
            RevenueLast30Days = s.Kpis.RevenueLast30Days,
            TopRisk = s.TopRiskLabel,
            NextRecommendedAction = s.NextRecommendedActionTitle,
            HasUpgradeRequest = upgradeSet.Contains(s.BusinessId),
            WonLeadLowHealth = wonLeadSet.Contains(s.BusinessId) && s.Score < 60
        }).ToList();

        IEnumerable<AdminCustomerSuccessRowViewModel> filtered = rows;

        if (!string.IsNullOrWhiteSpace(status))
        {
            filtered = filtered.Where(r => string.Equals(r.StatusLabel, MapStatusLabel(status), StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(churnRisk))
        {
            filtered = filtered.Where(r => string.Equals(r.ChurnRiskLabel, churnRisk, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(expansion))
        {
            filtered = filtered.Where(r => string.Equals(r.ExpansionLabel, expansion, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(subscriptionStatus))
        {
            filtered = filtered.Where(r => r.SubscriptionStatusText.Contains(subscriptionStatus, StringComparison.OrdinalIgnoreCase));
        }
        if (noOrdersLast30Days == true)
        {
            filtered = filtered.Where(r => r.OrdersLast30Days == 0);
        }
        if (hasUpgradeRequest == true)
        {
            filtered = filtered.Where(r => r.HasUpgradeRequest);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            filtered = filtered.Where(r =>
                r.BusinessName.Contains(q, StringComparison.OrdinalIgnoreCase)
                || r.Slug.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        var attentionList = rows
            .Where(r => r.StatusLabel is "Kritik" or "Riskli"
                || r.SubscriptionStatusText.Contains("Expired", StringComparison.OrdinalIgnoreCase)
                || r.OrdersLast30Days == 0
                || r.WonLeadLowHealth
                || r.TopRisk == "Aktif ürün")
            .OrderBy(r => r.Score)
            .Take(12)
            .Select(r => new AdminCustomerSuccessAttentionItem
            {
                BusinessId = r.BusinessId,
                BusinessName = r.BusinessName,
                Reason = r.WonLeadLowHealth
                    ? "Won lead ama kullanım sağlığı düşük"
                    : r.TopRisk ?? "Takip önerilir",
                SeverityBadgeClass = r.StatusLabel == "Kritik" ? "bg-danger" : "bg-warning text-dark"
            })
            .ToList();

        var model = new AdminCustomerSuccessViewModel
        {
            TotalActiveBusinesses = rows.Count,
            HealthyOrGrowthReadyCount = rows.Count(r => r.StatusLabel is "Sağlıklı" or "Büyümeye Hazır"),
            AtRiskOrCriticalCount = rows.Count(r => r.StatusLabel is "Kritik" or "Riskli"),
            NoOrdersLast30DaysCount = rows.Count(r => r.OrdersLast30Days == 0),
            ExpiringIn7DaysCount = snapshots.Count(s => s.Subscription.DaysRemaining.HasValue && s.Subscription.DaysRemaining.Value <= 7),
            UpgradeOpportunityCount = rows.Count(r => r.ExpansionLabel is "GoodFit" or "StrongFit"),
            WonLeadLowHealthCount = rows.Count(r => r.WonLeadLowHealth),
            StatusFilter = status,
            ChurnRiskFilter = churnRisk,
            ExpansionFilter = expansion,
            SubscriptionFilter = subscriptionStatus,
            NoOrdersLast30Days = noOrdersLast30Days,
            HasUpgradeRequest = hasUpgradeRequest,
            Search = search,
            Rows = filtered.OrderBy(r => r.Score).ThenBy(r => r.BusinessName).ToList(),
            AttentionList = attentionList
        };

        return View(model);
    }

    [HttpGet("Details/{businessId:int}")]
    public async Task<IActionResult> Details(int businessId, CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "customer-success";

        var snapshot = await _successHelper.BuildAsync(businessId, string.Empty, isBusinessOwner: true, cancellationToken);
        if (snapshot is null)
        {
            return NotFound();
        }

        snapshot.PublicMenuUrl = $"{Request.Scheme}://{Request.Host}/m/{snapshot.BusinessSlug}";

        var related = await _context.SalesRequests.AsNoTracking()
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(10)
            .Select(r => new AdminOnboardingRelatedSalesRequestViewModel
            {
                Id = r.Id,
                Status = r.Status,
                RequestType = r.RequestType,
                Source = r.Source,
                CreatedAtUtc = r.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var audits = await _context.AuditLogs.AsNoTracking()
            .Where(a => a.BusinessId == businessId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(8)
            .Select(a => new AdminOnboardingActivityItemViewModel
            {
                AtUtc = a.CreatedAtUtc,
                Title = a.Action,
                Detail = a.Summary
            })
            .ToListAsync(cancellationToken);

        var notifications = await _context.Notifications.AsNoTracking()
            .Where(n => n.BusinessId == businessId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(8)
            .Select(n => new AdminOnboardingActivityItemViewModel
            {
                AtUtc = n.CreatedAtUtc,
                Title = n.Title,
                Detail = n.Type
            })
            .ToListAsync(cancellationToken);

        var openStatuses = SupportTicketDisplayHelper.OpenStatuses;
        var supportTickets = await _context.SupportTickets.AsNoTracking()
            .Where(t => t.BusinessId == businessId && openStatuses.Contains(t.Status))
            .OrderByDescending(t => t.Priority == "Urgent")
            .ThenByDescending(t => t.Priority == "High")
            .ThenByDescending(t => t.CreatedAtUtc)
            .Take(10)
            .Select(t => new AdminSupportTicketRowViewModel
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                BusinessId = t.BusinessId,
                Subject = t.Subject,
                Category = t.Category,
                Priority = t.Priority,
                Status = t.Status,
                CreatedAtUtc = t.CreatedAtUtc,
                LastMessageAtUtc = t.LastMessageAtUtc,
                LastMessageByRole = t.LastMessageByRole
            })
            .ToListAsync(cancellationToken);

        return View(new AdminCustomerSuccessDetailsViewModel
        {
            Snapshot = snapshot,
            RelatedSalesRequests = related,
            RecentAudits = audits,
            RecentNotifications = notifications,
            OpenSupportTickets = supportTickets
        });
    }

    private static string MapStatusLabel(string status) => status.ToLowerInvariant() switch
    {
        "critical" => "Kritik",
        "atrisk" => "Riskli",
        "stable" => "Stabil",
        "healthy" => "Sağlıklı",
        "growthready" => "Büyümeye Hazır",
        _ => status
    };
}
