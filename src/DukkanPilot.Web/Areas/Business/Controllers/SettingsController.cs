using BusinessEntity = DukkanPilot.Core.Entities.Business;
using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Settings")]
[Authorize(Roles = nameof(UserRole.BusinessOwner))]
public class SettingsController : BusinessBaseController
{
    private readonly AppDbContext _context;

    public SettingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "settings";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var model = await BuildViewModelAsync(businessId);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(BusinessSettingsViewModel model)
    {
        ViewData["ActiveMenu"] = "settings";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (!ModelState.IsValid)
        {
            model.Slug = await GetBusinessSlugAsync(businessId) ?? model.Slug;
            model.BusinessId = businessId;
            model.IsActive = await GetBusinessIsActiveAsync(businessId);
            return View(model);
        }

        var business = await _context.Businesses
            .Include(b => b.Setting)
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business is null)
        {
            return NotFound();
        }

        business.Name = model.BusinessName.Trim();
        business.Phone = TrimToNull(model.Phone);
        business.LogoUrl = TrimToNull(model.LogoUrl);
        business.Address = TrimToNull(model.Address);
        business.Description = TrimToNull(model.Description);
        business.UpdatedAt = DateTime.UtcNow;

        if (business.Setting is null)
        {
            business.Setting = new BusinessSetting
            {
                BusinessId = businessId,
                WhatsAppNumber = TrimToNull(model.WhatsAppNumber),
                ThemeColor = model.ThemeColor.Trim(),
                Currency = model.Currency.Trim().ToUpperInvariant()
            };
        }
        else
        {
            business.Setting.WhatsAppNumber = TrimToNull(model.WhatsAppNumber);
            business.Setting.ThemeColor = model.ThemeColor.Trim();
            business.Setting.Currency = model.Currency.Trim().ToUpperInvariant();
            business.Setting.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "İşletme ayarları başarıyla kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<BusinessSettingsViewModel?> BuildViewModelAsync(int businessId)
    {
        var business = await _context.Businesses
            .AsNoTracking()
            .Include(b => b.Setting)
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business is null)
        {
            return null;
        }

        return MapToViewModel(business);
    }

    private static BusinessSettingsViewModel MapToViewModel(BusinessEntity business)
    {
        return new BusinessSettingsViewModel
        {
            BusinessId = business.Id,
            BusinessName = business.Name,
            Slug = business.Slug,
            Phone = business.Phone,
            LogoUrl = business.LogoUrl,
            Address = business.Address,
            Description = business.Description,
            IsActive = business.IsActive,
            WhatsAppNumber = business.Setting?.WhatsAppNumber,
            Currency = business.Setting?.Currency ?? "TRY",
            ThemeColor = business.Setting?.ThemeColor ?? "#2563eb"
        };
    }

    private async Task<string?> GetBusinessSlugAsync(int businessId)
    {
        return await _context.Businesses
            .AsNoTracking()
            .Where(b => b.Id == businessId)
            .Select(b => b.Slug)
            .FirstOrDefaultAsync();
    }

    private async Task<bool> GetBusinessIsActiveAsync(int businessId)
    {
        return await _context.Businesses
            .AsNoTracking()
            .Where(b => b.Id == businessId)
            .Select(b => b.IsActive)
            .FirstOrDefaultAsync();
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
