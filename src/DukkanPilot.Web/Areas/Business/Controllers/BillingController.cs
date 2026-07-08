using System.Globalization;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Billing")]
public class BillingController : BusinessBaseController
{
    private readonly AppDbContext _context;
    private readonly BusinessSubscriptionStatusHelper _subscriptionStatusHelper;
    private readonly BusinessPlanLimitHelper _planLimitHelper;
    private readonly ISalesRequestService _salesRequests;

    public BillingController(
        AppDbContext context,
        BusinessSubscriptionStatusHelper subscriptionStatusHelper,
        BusinessPlanLimitHelper planLimitHelper,
        ISalesRequestService salesRequests)
    {
        _context = context;
        _subscriptionStatusHelper = subscriptionStatusHelper;
        _planLimitHelper = planLimitHelper;
        _salesRequests = salesRequests;
    }

    [HttpGet("")]
    [Authorize(Roles = nameof(UserRole.BusinessOwner))]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "billing";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var subscription = await _subscriptionStatusHelper.GetStatusAsync(businessId);
        var currentPlanId = await GetCurrentPlanIdAsync(businessId);
        var activePlans = await _context.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();

        var model = new BillingIndexViewModel
        {
            Subscription = subscription,
            Usage = await _planLimitHelper.GetUsageAsync(businessId),
            AvailablePlans = activePlans
                .Select(p => _planLimitHelper.MapToAvailablePlan(p, currentPlanId))
                .ToList()
        };

        return View(model);
    }

    [HttpGet("Requests")]
    [Authorize(Roles = nameof(UserRole.BusinessOwner))]
    public async Task<IActionResult> Requests(CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "billing-requests";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var items = await _salesRequests.GetBusinessRequestsAsync(businessId, cancellationToken);
        var model = new BusinessSalesRequestListViewModel
        {
            Items = items.Select(r => new BusinessSalesRequestRowViewModel
            {
                Id = r.Id,
                CreatedAtUtc = r.CreatedAtUtc,
                RequestType = r.RequestType,
                Status = r.Status,
                Priority = r.Priority,
                CurrentPlanName = r.CurrentPlanName,
                RequestedPlanName = r.RequestedPlanName,
                Message = r.Message,
                UpdatedAtUtc = r.UpdatedAtUtc,
                ClosedAtUtc = r.ClosedAtUtc
            }).ToList()
        };

        return View(model);
    }

    [HttpGet("RequestUpgrade/{planId:int}")]
    [Authorize(Roles = nameof(UserRole.BusinessOwner))]
    public async Task<IActionResult> RequestUpgrade(int planId)
    {
        ViewData["ActiveMenu"] = "billing";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var model = await BuildUpgradeRequestViewModelAsync(businessId, planId);
        if (model is null)
        {
            return NotFound();
        }

        var currentPlanId = await GetCurrentPlanIdAsync(businessId);
        if (currentPlanId == planId)
        {
            TempData["Error"] = "Mevcut planınız için yükseltme talebi oluşturulamaz.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpPost("RequestUpgrade/{planId:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = nameof(UserRole.BusinessOwner))]
    public async Task<IActionResult> RequestUpgrade(int planId, PlanUpgradeRequestViewModel model, CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "billing";

        if (planId != model.RequestedPlanId)
        {
            return BadRequest();
        }

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var builtModel = await BuildUpgradeRequestViewModelAsync(businessId, planId);
        if (builtModel is null)
        {
            return NotFound();
        }

        var currentPlanId = await GetCurrentPlanIdAsync(businessId);
        if (currentPlanId == planId)
        {
            TempData["Error"] = "Mevcut planınız için yükseltme talebi oluşturulamaz.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _salesRequests.CreateBusinessPlanRequestAsync(new BusinessSalesRequestCreateInput
        {
            BusinessId = businessId,
            BusinessName = builtModel.BusinessName,
            ContactName = CurrentUserEmail ?? builtModel.BusinessName,
            Email = builtModel.OwnerEmail,
            CurrentPlanId = currentPlanId,
            CurrentPlanName = builtModel.CurrentPlanName,
            RequestedPlanId = builtModel.RequestedPlanId,
            RequestedPlanName = builtModel.RequestedPlanName,
            Message = builtModel.RequestMessage
        }, cancellationToken);

        if (result.WasDuplicate)
        {
            TempData["Error"] = "Bu plan için açık talebiniz var";
            return RedirectToAction(nameof(Requests));
        }

        builtModel.RequestMessage = BuildRequestMessage(builtModel, result.Request.Id);
        TempData["UpgradeRequestMessage"] = builtModel.RequestMessage;
        TempData["SalesRequestId"] = result.Request.Id.ToString();

        return RedirectToAction(nameof(RequestUpgradeConfirmation));
    }

    [HttpGet("RequestUpgradeConfirmation")]
    [Authorize(Roles = nameof(UserRole.BusinessOwner))]
    public IActionResult RequestUpgradeConfirmation()
    {
        ViewData["ActiveMenu"] = "billing";

        var message = TempData["UpgradeRequestMessage"] as string;
        if (string.IsNullOrWhiteSpace(message))
        {
            return RedirectToAction(nameof(Index));
        }

        int? salesRequestId = int.TryParse(TempData["SalesRequestId"]?.ToString(), out var parsedId)
            ? parsedId
            : null;

        return View(new PlanUpgradeRequestViewModel
        {
            RequestMessage = message,
            SalesRequestId = salesRequestId
        });
    }

    [HttpGet("Required")]
    public IActionResult Required()
    {
        ViewData["ActiveMenu"] = "billing-required";
        ViewData["IsBusinessOwner"] = User.IsInRole(nameof(UserRole.BusinessOwner));
        return View();
    }

    private async Task<PlanUpgradeRequestViewModel?> BuildUpgradeRequestViewModelAsync(int businessId, int planId)
    {
        var requestedPlan = await _context.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId && p.IsActive);

        if (requestedPlan is null)
        {
            return null;
        }

        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business is null)
        {
            return null;
        }

        var subscription = await _subscriptionStatusHelper.GetStatusAsync(businessId);

        return new PlanUpgradeRequestViewModel
        {
            BusinessId = businessId,
            BusinessName = business.Name,
            BusinessSlug = business.Slug,
            OwnerEmail = CurrentUserEmail ?? "-",
            CurrentPlanName = subscription.PlanName,
            RequestedPlanId = requestedPlan.Id,
            RequestedPlanName = requestedPlan.Name,
            RequestedPlanPrice = requestedPlan.Price,
            RequestedAtUtc = DateTime.UtcNow,
            RequestMessage = string.Empty
        };
    }

    private async Task<int?> GetCurrentPlanIdAsync(int businessId)
    {
        return await _context.BusinessSubscriptions
            .AsNoTracking()
            .Where(s => s.BusinessId == businessId && s.IsActive)
            .OrderByDescending(s => s.Status == SubscriptionStatus.Active)
            .ThenByDescending(s => s.Status == SubscriptionStatus.Trial)
            .ThenByDescending(s => s.StartDate)
            .ThenByDescending(s => s.CreatedAt)
            .Select(s => (int?)s.SubscriptionPlanId)
            .FirstOrDefaultAsync();
    }

    private static string BuildRequestMessage(PlanUpgradeRequestViewModel model, int salesRequestId)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var requestedAtLocal = model.RequestedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", culture);

        return $"""
            Plan yükseltme talebi:
            Talep No: #{salesRequestId}
            İşletme: {model.BusinessName} ({model.BusinessSlug})
            Owner: {model.OwnerEmail}
            Mevcut Plan: {model.CurrentPlanName}
            İstenen Plan: {model.RequestedPlanName} ({model.RequestedPlanPrice:N2} ₺)
            Talep Tarihi: {requestedAtLocal}
            Not: Admin panelinden /Admin/SalesRequests/Details/{salesRequestId} veya /Admin/Businesses/Subscription/{model.BusinessId} üzerinden takip/güncelleme yapılır.
            """;
    }
}
