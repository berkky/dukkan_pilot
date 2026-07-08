using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Admin.Models;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Route("Admin/SalesRequests")]
public class SalesRequestsController : AdminBaseController
{
    private readonly AppDbContext _context;
    private readonly ISalesRequestService _salesRequests;
    private readonly CustomerOnboardingHelper _onboardingHelper;
    private readonly CustomerSuccessHealthHelper _successHelper;

    public SalesRequestsController(
        AppDbContext context,
        ISalesRequestService salesRequests,
        CustomerOnboardingHelper onboardingHelper,
        CustomerSuccessHealthHelper successHelper)
    {
        _context = context;
        _salesRequests = salesRequests;
        _onboardingHelper = onboardingHelper;
        _successHelper = successHelper;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? status,
        string? requestType,
        string? source,
        string? priority,
        string? search,
        int? planId,
        CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "sales-requests";

        var summary = await _salesRequests.GetAdminSummaryAsync(cancellationToken);

        var query = _context.SalesRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(requestType))
        {
            query = query.Where(r => r.RequestType == requestType);
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            query = query.Where(r => r.Source == source);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(r => r.Priority == priority);
        }

        if (planId is int pid)
        {
            query = query.Where(r => r.RequestedPlanId == pid);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                (r.ContactName != null && r.ContactName.Contains(term))
                || (r.BusinessName != null && r.BusinessName.Contains(term))
                || (r.Email != null && r.Email.Contains(term))
                || (r.Phone != null && r.Phone.Contains(term))
                || (r.RequestedPlanName != null && r.RequestedPlanName.Contains(term))
                || (r.Message != null && r.Message.Contains(term)));
        }

        var items = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(300)
            .Select(r => new AdminSalesRequestRowViewModel
            {
                Id = r.Id,
                CreatedAtUtc = r.CreatedAtUtc,
                Source = r.Source,
                RequestType = r.RequestType,
                Status = r.Status,
                Priority = r.Priority,
                ContactName = r.ContactName,
                BusinessName = r.BusinessName,
                Email = r.Email,
                RequestedPlanName = r.RequestedPlanName,
                BusinessId = r.BusinessId,
                RequestedPlanId = r.RequestedPlanId
            })
            .ToListAsync(cancellationToken);

        var model = new AdminSalesRequestListViewModel
        {
            Summary = summary,
            Status = status,
            RequestType = requestType,
            Source = source,
            Priority = priority,
            Search = search,
            PlanId = planId,
            Items = items
        };

        return View(model);
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "sales-requests";

        var entity = await _salesRequests.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        var model = MapDetail(entity);

        if (entity.BusinessId is int businessId)
        {
            var snap = await _onboardingHelper.BuildAsync(businessId, string.Empty, isBusinessOwner: true, cancellationToken);
            if (snap is not null)
            {
                model.OnboardingScore = snap.Score;
                model.OnboardingStatusLabel = snap.StatusLabel;
                model.OnboardingBadgeClass = snap.StatusBadgeClass;
                model.OnboardingNextAction = snap.NextBestActionTitle;
            }

            var success = await _successHelper.BuildAsync(businessId, string.Empty, isBusinessOwner: true, cancellationToken);
            if (success is not null)
            {
                model.CustomerSuccessScore = success.Score;
                model.CustomerSuccessStatusLabel = success.StatusLabel;
                model.CustomerSuccessBadgeClass = success.StatusBadgeClass;
                model.CustomerSuccessTopRisk = success.TopRiskLabel;
            }
        }

        return View(model);
    }

    [HttpPost("UpdateStatus/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, AdminSalesRequestUpdateViewModel model, CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "sales-requests";

        if (!SalesRequestDisplayHelper.IsAllowedStatus(model.Status))
        {
            TempData["Error"] = "Geçersiz durum seçildi.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!SalesRequestDisplayHelper.AllowedPriorities.Contains(model.Priority, StringComparer.Ordinal))
        {
            model.Priority = "Normal";
        }

        var updated = await _salesRequests.UpdateStatusAsync(new SalesRequestStatusUpdateInput
        {
            Id = id,
            Status = model.Status,
            Priority = model.Priority,
            AdminNotes = model.AdminNotes,
            ClosedReason = model.ClosedReason,
            MarkContactedNow = model.MarkContactedNow
        }, cancellationToken);

        if (updated is null)
        {
            TempData["Error"] = "Talep bulunamadı veya durum güncellenemedi.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Satış talebi güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private static AdminSalesRequestDetailViewModel MapDetail(Core.Entities.SalesRequest entity) => new()
    {
        Id = entity.Id,
        CreatedAtUtc = entity.CreatedAtUtc,
        UpdatedAtUtc = entity.UpdatedAtUtc,
        BusinessId = entity.BusinessId,
        BusinessName = entity.BusinessName,
        Source = entity.Source,
        RequestType = entity.RequestType,
        Status = entity.Status,
        Priority = entity.Priority,
        ContactName = entity.ContactName,
        Email = entity.Email,
        Phone = entity.Phone,
        CurrentPlanName = entity.CurrentPlanName,
        CurrentPlanId = entity.CurrentPlanId,
        RequestedPlanName = entity.RequestedPlanName,
        RequestedPlanId = entity.RequestedPlanId,
        Message = entity.Message,
        AdminNotes = entity.AdminNotes,
        LastContactedAtUtc = entity.LastContactedAtUtc,
        ClosedAtUtc = entity.ClosedAtUtc,
        ClosedReason = entity.ClosedReason,
        IpAddress = entity.IpAddress,
        UserAgent = entity.UserAgent,
        PrivacyNoticeAcknowledged = entity.PrivacyNoticeAcknowledged,
        KvkkNoticeAcknowledged = entity.KvkkNoticeAcknowledged,
        Update = new AdminSalesRequestUpdateViewModel
        {
            Status = entity.Status,
            Priority = entity.Priority,
            AdminNotes = entity.AdminNotes,
            ClosedReason = entity.ClosedReason
        }
    };
}
