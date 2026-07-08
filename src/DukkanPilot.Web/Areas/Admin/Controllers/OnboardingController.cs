using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Admin.Models;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Models.Onboarding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Route("Admin/Onboarding")]
public class OnboardingController : AdminBaseController
{
    private readonly AppDbContext _context;
    private readonly CustomerOnboardingHelper _onboardingHelper;

    private static readonly HashSet<string> OpenSalesStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "New", "Contacted", "Qualified", "WaitingCustomer"
    };

    private static readonly HashSet<string> HandoffStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Won", "Qualified"
    };

    public OnboardingController(AppDbContext context, CustomerOnboardingHelper onboardingHelper)
    {
        _context = context;
        _onboardingHelper = onboardingHelper;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? status = null,
        string? search = null,
        int? minScore = null,
        int? maxScore = null,
        bool? hasOpenSalesRequest = null,
        bool? hasNoActiveProduct = null,
        bool? hasNoOrder = null)
    {
        ViewData["ActiveMenu"] = "onboarding";

        var businessIds = await _context.Businesses.AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => b.Id)
            .ToListAsync();

        var snapshots = await _onboardingHelper.BuildForBusinessesAsync(businessIds);

        var openSalesBusinessIds = await _context.SalesRequests.AsNoTracking()
            .Where(r => r.BusinessId != null && OpenSalesStatuses.Contains(r.Status))
            .Select(r => r.BusinessId!.Value)
            .Distinct()
            .ToListAsync();

        var openSet = openSalesBusinessIds.ToHashSet();

        var rows = snapshots.Select(s => new AdminOnboardingRowViewModel
        {
            BusinessId = s.BusinessId,
            BusinessName = s.BusinessName,
            Slug = s.BusinessSlug,
            PublicMenuUrl = s.PublicMenuUrl,
            PlanName = s.PlanName,
            SubscriptionStatusLabel = s.SubscriptionStatusLabel,
            Score = s.Score,
            StatusLabel = s.StatusLabel,
            StatusBadgeClass = s.StatusBadgeClass,
            Status = s.Status,
            MissingRequiredCount = s.MissingRequiredCount,
            NextBestActionTitle = s.NextBestActionTitle,
            ActiveProductCount = s.ActiveProductCount,
            OrderCount = s.OrderCount,
            CampaignCount = s.CampaignCount,
            LastActivityAtUtc = s.LastActivityAtUtc,
            IsAtRisk = s.IsAtRisk,
            HasOpenSalesRequest = openSet.Contains(s.BusinessId)
        }).ToList();

        var filtered = rows.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<OnboardingStatus>(status, ignoreCase: true, out var statusEnum))
        {
            filtered = filtered.Where(r => r.Status == statusEnum);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            filtered = filtered.Where(r =>
                r.BusinessName.Contains(q, StringComparison.OrdinalIgnoreCase)
                || r.Slug.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        if (minScore is int min)
        {
            filtered = filtered.Where(r => r.Score >= min);
        }

        if (maxScore is int max)
        {
            filtered = filtered.Where(r => r.Score <= max);
        }

        if (hasOpenSalesRequest == true)
        {
            filtered = filtered.Where(r => r.HasOpenSalesRequest);
        }

        if (hasNoActiveProduct == true)
        {
            filtered = filtered.Where(r => r.ActiveProductCount == 0);
        }

        if (hasNoOrder == true)
        {
            filtered = filtered.Where(r => r.OrderCount == 0);
        }

        var wonSince = DateTime.UtcNow.AddDays(-7);
        var wonLast7 = await _context.SalesRequests.AsNoTracking()
            .CountAsync(r => r.Status == "Won"
                && (r.ClosedAtUtc ?? r.UpdatedAtUtc ?? r.CreatedAtUtc) >= wonSince);

        var handoffRequests = await _context.SalesRequests.AsNoTracking()
            .Where(r => HandoffStatuses.Contains(r.Status))
            .OrderByDescending(r => r.UpdatedAtUtc ?? r.CreatedAtUtc)
            .Take(30)
            .ToListAsync();

        var snapById = snapshots.ToDictionary(s => s.BusinessId);
        var handoffs = handoffRequests.Select(r =>
        {
            CustomerOnboardingSnapshot? linked = null;
            if (r.BusinessId is int bid)
            {
                snapById.TryGetValue(bid, out linked);
            }

            return new AdminOnboardingHandoffRowViewModel
            {
                SalesRequestId = r.Id,
                Status = r.Status,
                ContactName = r.ContactName ?? "-",
                BusinessNameFromRequest = r.BusinessName,
                BusinessId = r.BusinessId,
                LinkedBusinessName = linked?.BusinessName,
                OnboardingScore = linked?.Score,
                OnboardingStatusLabel = linked?.StatusLabel,
                OnboardingBadgeClass = linked?.StatusBadgeClass,
                CreatedAtUtc = r.CreatedAtUtc
            };
        }).ToList();

        var model = new AdminOnboardingViewModel
        {
            TotalBusinesses = rows.Count,
            SetupInProgressCount = rows.Count(r => r.Status == OnboardingStatus.SetupInProgress),
            AlmostReadyCount = rows.Count(r => r.Status == OnboardingStatus.AlmostReady),
            ReadyToLaunchCount = rows.Count(r => r.Status == OnboardingStatus.ReadyToLaunch),
            LiveCount = rows.Count(r => r.Status == OnboardingStatus.Live),
            AtRiskCount = rows.Count(r => r.IsAtRisk),
            WonSalesLast7Days = wonLast7,
            StatusFilter = status,
            Search = search,
            MinScore = minScore,
            MaxScore = maxScore,
            HasOpenSalesRequest = hasOpenSalesRequest,
            HasNoActiveProduct = hasNoActiveProduct,
            HasNoOrder = hasNoOrder,
            Rows = filtered
                .OrderBy(r => r.Score)
                .ThenBy(r => r.BusinessName)
                .ToList(),
            SalesHandoffs = handoffs
        };

        return View(model);
    }

    [HttpGet("Details/{businessId:int}")]
    public async Task<IActionResult> Details(int businessId)
    {
        ViewData["ActiveMenu"] = "onboarding";

        var snapshot = await _onboardingHelper.BuildAsync(businessId, $"/m/", isBusinessOwner: true);
        if (snapshot is null)
        {
            return NotFound();
        }

        // Absolute public URL for admin convenience
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
            .ToListAsync();

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
            .ToListAsync();

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
            .ToListAsync();

        return View(new AdminOnboardingDetailViewModel
        {
            Snapshot = snapshot,
            RelatedSalesRequests = related,
            RecentAudits = audits,
            RecentNotifications = notifications
        });
    }
}
