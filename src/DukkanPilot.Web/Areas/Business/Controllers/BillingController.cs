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
    private readonly IAuditLogService _auditLog;

    public BillingController(
        AppDbContext context,
        BusinessSubscriptionStatusHelper subscriptionStatusHelper,
        BusinessPlanLimitHelper planLimitHelper,
        IAuditLogService auditLog)
    {
        _context = context;
        _subscriptionStatusHelper = subscriptionStatusHelper;
        _planLimitHelper = planLimitHelper;
        _auditLog = auditLog;
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
    public async Task<IActionResult> RequestUpgrade(int planId, PlanUpgradeRequestViewModel model)
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

        builtModel.RequestMessage = BuildRequestMessage(builtModel);
        TempData["UpgradeRequestMessage"] = builtModel.RequestMessage;

        await _auditLog.LogBusinessAsync(
            businessId,
            "Subscription.UpgradeRequested",
            "BusinessSubscription",
            planId,
            $"Plan yükseltme talebi oluşturuldu: {builtModel.CurrentPlanName} → {builtModel.RequestedPlanName}",
            new { requestedPlanId = planId, requestedPlanName = builtModel.RequestedPlanName });

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

        return View(new PlanUpgradeRequestViewModel
        {
            RequestMessage = message
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

    private static string BuildRequestMessage(PlanUpgradeRequestViewModel model)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var requestedAtLocal = model.RequestedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", culture);

        return $"""
            Plan yükseltme talebi:
            İşletme: {model.BusinessName} ({model.BusinessSlug})
            Owner: {model.OwnerEmail}
            Mevcut Plan: {model.CurrentPlanName}
            İstenen Plan: {model.RequestedPlanName} ({model.RequestedPlanPrice:N2} ₺)
            Talep Tarihi: {requestedAtLocal}
            Not: Admin panelinden /Admin/Businesses/Subscription/{model.BusinessId} ekranı üzerinden manuel güncelleme yapılmalıdır.
            """;
    }
}
