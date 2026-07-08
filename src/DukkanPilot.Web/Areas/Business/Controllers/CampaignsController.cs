using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Filters;
using DukkanPilot.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Campaigns")]
[RequireActiveSubscription]
public class CampaignsController : BusinessBaseController
{
    private readonly AppDbContext _context;
    private readonly BusinessPlanLimitHelper _planLimitHelper;

    public CampaignsController(AppDbContext context, BusinessPlanLimitHelper planLimitHelper)
    {
        _context = context;
        _planLimitHelper = planLimitHelper;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "campaigns";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var now = DateTime.UtcNow;
        var items = await _context.Campaigns
            .AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CampaignListViewModel
            {
                Id = c.Id,
                Title = c.Title,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                IsActive = c.IsActive,
                IsPublished = c.IsActive
                    && c.StartDate <= now
                    && (c.EndDate == null || c.EndDate >= now),
                DiscountType = c.DiscountType,
                DiscountValue = c.DiscountValue,
                MinimumOrderAmount = c.MinimumOrderAmount,
                IsAutoApply = c.IsAutoApply,
                IsPublicVisible = c.IsPublicVisible,
                Priority = c.Priority
            })
            .ToListAsync();

        return View(items);
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        ViewData["ActiveMenu"] = "campaigns-create";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        await PlanLimitViewDataHelper.SetLimitWarningAsync(this, _planLimitHelper, businessId, PlanLimitResource.Campaigns);

        return View(new CampaignFormViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CampaignFormViewModel model)
    {
        ViewData["ActiveMenu"] = "campaigns-create";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (await _planLimitHelper.IsLimitReachedAsync(businessId, PlanLimitResource.Campaigns))
        {
            ModelState.AddModelError(string.Empty,
                _planLimitHelper.GetLimitReachedMessage(PlanLimitResource.Campaigns, User.IsInRole(nameof(UserRole.BusinessOwner))));
        }

        if (!await IsCampaignTitleAvailableAsync(businessId, model.Title))
        {
            ModelState.AddModelError(nameof(model.Title), "Bu kampanya başlığı zaten kullanılıyor.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var campaign = new Campaign
        {
            BusinessId = businessId,
            Title = model.Title.Trim(),
            Description = TrimToNull(model.Description),
            StartDate = ToStartOfUtcDay(model.StartDate),
            EndDate = ToEndOfUtcDay(model.EndDate),
            IsActive = model.IsActive,
            DiscountType = model.DiscountType,
            DiscountValue = model.DiscountValue,
            MinimumOrderAmount = model.MinimumOrderAmount,
            MaximumDiscountAmount = model.MaximumDiscountAmount,
            IsPublicVisible = model.IsPublicVisible,
            IsAutoApply = model.IsAutoApply,
            Priority = model.Priority
        };

        _context.Campaigns.Add(campaign);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Kampanya başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveMenu"] = "campaigns";

        var model = await BuildDetailsViewModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["ActiveMenu"] = "campaigns";

        var model = await BuildFormViewModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CampaignFormViewModel model)
    {
        ViewData["ActiveMenu"] = "campaigns";

        if (id != model.Id)
        {
            return BadRequest();
        }

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (!await IsCampaignTitleAvailableAsync(businessId, model.Title, id))
        {
            ModelState.AddModelError(nameof(model.Title), "Bu kampanya başlığı zaten kullanılıyor.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var campaign = await _context.Campaigns
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        if (campaign is null)
        {
            return NotFound();
        }

        campaign.Title = model.Title.Trim();
        campaign.Description = TrimToNull(model.Description);
        campaign.StartDate = ToStartOfUtcDay(model.StartDate);
        campaign.EndDate = ToEndOfUtcDay(model.EndDate);
        campaign.IsActive = model.IsActive;
        campaign.DiscountType = model.DiscountType;
        campaign.DiscountValue = model.DiscountValue;
        campaign.MinimumOrderAmount = model.MinimumOrderAmount;
        campaign.MaximumDiscountAmount = model.MaximumDiscountAmount;
        campaign.IsPublicVisible = model.IsPublicVisible;
        campaign.IsAutoApply = model.IsAutoApply;
        campaign.Priority = model.Priority;
        campaign.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Kampanya başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        ViewData["ActiveMenu"] = "campaigns";

        var model = await BuildDetailsViewModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost("Delete/{id:int}")]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var campaign = await _context.Campaigns
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        if (campaign is null)
        {
            return NotFound();
        }

        campaign.IsActive = false;
        campaign.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Kampanya pasif duruma alındı.";
        return RedirectToAction(nameof(Index));
    }
    private async Task<bool> IsCampaignTitleAvailableAsync(int businessId, string title, int? excludeId = null)
    {
        var normalizedTitle = title.Trim();
        return !await _context.Campaigns.AnyAsync(c =>
            c.BusinessId == businessId &&
            c.Title == normalizedTitle &&
            (!excludeId.HasValue || c.Id != excludeId.Value));
    }

    private async Task<CampaignFormViewModel?> BuildFormViewModelAsync(int id)
    {
        var businessId = GetCurrentBusinessId();
        if (businessId is null)
        {
            return null;
        }

        var campaign = await _context.Campaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        if (campaign is null)
        {
            return null;
        }

        return new CampaignFormViewModel
        {
            Id = campaign.Id,
            Title = campaign.Title,
            Description = campaign.Description,
            StartDate = campaign.StartDate.Date,
            EndDate = (campaign.EndDate ?? campaign.StartDate).Date,
            IsActive = campaign.IsActive,
            DiscountType = campaign.DiscountType,
            DiscountValue = campaign.DiscountValue,
            MinimumOrderAmount = campaign.MinimumOrderAmount,
            MaximumDiscountAmount = campaign.MaximumDiscountAmount,
            IsPublicVisible = campaign.IsPublicVisible,
            IsAutoApply = campaign.IsAutoApply,
            Priority = campaign.Priority
        };
    }

    private async Task<CampaignDetailsViewModel?> BuildDetailsViewModelAsync(int id)
    {
        var businessId = GetCurrentBusinessId();
        if (businessId is null)
        {
            return null;
        }

        var campaign = await _context.Campaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        if (campaign is null)
        {
            return null;
        }

        var campaignOrders = _context.Orders.AsNoTracking()
            .Where(o => o.BusinessId == businessId
                && o.AppliedCampaignId == campaign.Id
                && o.Status != OrderStatus.Cancelled);

        var orderCount = await campaignOrders.CountAsync();
        var totalDiscount = await campaignOrders.SumAsync(o => (decimal?)o.DiscountAmount) ?? 0m;
        var netRevenue = await campaignOrders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
        var lastUsedAt = await campaignOrders.MaxAsync(o => (DateTime?)o.CreatedAt);
        var recentOrders = await campaignOrders
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => new CampaignPerformanceOrderRowViewModel
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                CreatedAt = o.CreatedAt,
                CustomerName = o.CustomerName,
                SubtotalAmount = o.SubtotalAmount,
                DiscountAmount = o.DiscountAmount,
                TotalAmount = o.TotalAmount,
                Status = o.Status
            })
            .ToListAsync();

        return new CampaignDetailsViewModel
        {
            Id = campaign.Id,
            Title = campaign.Title,
            Description = campaign.Description,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            IsActive = campaign.IsActive,
            IsPublished = CampaignDisplayHelper.IsPublished(campaign.IsActive, campaign.StartDate, campaign.EndDate),
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt,
            DiscountType = campaign.DiscountType,
            DiscountValue = campaign.DiscountValue,
            MinimumOrderAmount = campaign.MinimumOrderAmount,
            MaximumDiscountAmount = campaign.MaximumDiscountAmount,
            IsPublicVisible = campaign.IsPublicVisible,
            IsAutoApply = campaign.IsAutoApply,
            Priority = campaign.Priority,
            Performance = new CampaignPerformanceSummaryViewModel
            {
                OrderCount = orderCount,
                TotalDiscount = totalDiscount,
                NetRevenue = netRevenue,
                AverageBasket = orderCount > 0 ? netRevenue / orderCount : 0m,
                LastUsedAt = lastUsedAt,
                RecentOrders = recentOrders
            }
        };
    }

    private static DateTime ToStartOfUtcDay(DateTime date)
    {
        return DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
    }

    private static DateTime ToEndOfUtcDay(DateTime date)
    {
        return DateTime.SpecifyKind(date.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
    }

    private static string? TrimToNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
