using System.Text.RegularExpressions;
using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Filters;
using DukkanPilot.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/QrMenu")]
[RequireActiveSubscription]
public class QrMenuController : BusinessBaseController
{
    private readonly AppDbContext _context;
    private readonly BusinessPlanLimitHelper _planLimitHelper;

    public QrMenuController(AppDbContext context, BusinessPlanLimitHelper planLimitHelper)
    {
        _context = context;
        _planLimitHelper = planLimitHelper;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "qr-menu";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var business = await _context.Businesses
            .AsNoTracking()
            .Include(b => b.Setting)
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business is null)
        {
            return NotFound();
        }

        var usage = await _planLimitHelper.GetUsageAsync(businessId);
        var isBusinessOwner = User.IsInRole(nameof(UserRole.BusinessOwner));
        var qrLimitReached = usage.QrCodes.IsLimitReached;

        var publicMenuUrl = $"{Request.Scheme}://{Request.Host}/m/{business.Slug}";
        var shareMessage = $"Merhaba, QR menümüzü buradan inceleyebilirsiniz: {publicMenuUrl}";
        var whatsAppShareUrl = $"https://wa.me/?text={Uri.EscapeDataString(shareMessage)}";

        var latestQrCode = await _context.QrCodes
            .AsNoTracking()
            .Where(q => q.BusinessId == businessId && q.IsActive)
            .OrderByDescending(q => q.LastGeneratedAt ?? q.CreatedAt)
            .FirstOrDefaultAsync();

        var model = new QrMenuViewModel
        {
            BusinessName = business.Name,
            Slug = business.Slug,
            PublicMenuUrl = publicMenuUrl,
            WhatsAppShareUrl = whatsAppShareUrl,
            ThemeColor = ResolveThemeColor(business.Setting?.ThemeColor),
            LogoUrl = business.LogoUrl,
            Description = business.Description,
            HasQrCode = latestQrCode is not null,
            LastGeneratedAt = latestQrCode?.LastGeneratedAt ?? latestQrCode?.CreatedAt,
            QrCodesUsed = usage.QrCodes.Used,
            QrLimitReached = qrLimitReached,
            QrLimitMessage = qrLimitReached
                ? _planLimitHelper.GetLimitReachedMessage(PlanLimitResource.QrCodes, isBusinessOwner)
                : null
        };

        ViewData["IsBusinessOwner"] = isBusinessOwner;

        return View(model);
    }

    [HttpGet("Print")]
    public async Task<IActionResult> Print()
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var business = await _context.Businesses
            .AsNoTracking()
            .Include(b => b.Setting)
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business is null)
        {
            return NotFound();
        }

        var publicMenuUrl = $"{Request.Scheme}://{Request.Host}/m/{business.Slug}";

        var model = new QrMenuPrintViewModel
        {
            BusinessName = business.Name,
            BusinessSlug = business.Slug,
            PublicMenuUrl = publicMenuUrl,
            LogoUrl = business.LogoUrl,
            Description = business.Description,
            Address = business.Address,
            Phone = business.Phone,
            ThemeColor = ResolveThemeColor(business.Setting?.ThemeColor),
            Currency = string.IsNullOrWhiteSpace(business.Setting?.Currency)
                ? "TRY"
                : business.Setting.Currency.Trim().ToUpperInvariant(),
            PrintTitle = business.Name,
            PrintSubtitle = string.IsNullOrWhiteSpace(business.Description)
                ? "Dijital Menü"
                : business.Description.Trim()
        };

        return View(model);
    }

    [HttpPost("Generate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate()
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var isBusinessOwner = User.IsInRole(nameof(UserRole.BusinessOwner));
        if (await _planLimitHelper.IsLimitReachedAsync(businessId, PlanLimitResource.QrCodes))
        {
            TempData["Error"] = _planLimitHelper.GetLimitReachedMessage(PlanLimitResource.QrCodes, isBusinessOwner);
            return RedirectToAction(nameof(Index));
        }

        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business is null)
        {
            return NotFound();
        }

        var publicMenuUrl = $"{Request.Scheme}://{Request.Host}/m/{business.Slug}";
        var qrCount = await _context.QrCodes.CountAsync(q => q.BusinessId == businessId);

        _context.QrCodes.Add(new QrCode
        {
            BusinessId = businessId,
            Label = qrCount == 0 ? "Ana Menü" : $"QR {qrCount + 1}",
            TargetUrl = publicMenuUrl,
            LastGeneratedAt = DateTime.UtcNow,
            IsActive = true
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = "QR kaydı başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    private static string ResolveThemeColor(string? themeColor)
    {
        const string defaultColor = "#2563eb";

        if (string.IsNullOrWhiteSpace(themeColor))
        {
            return defaultColor;
        }

        return Regex.IsMatch(themeColor.Trim(), @"^#[0-9A-Fa-f]{6}$")
            ? themeColor.Trim()
            : defaultColor;
    }
}
