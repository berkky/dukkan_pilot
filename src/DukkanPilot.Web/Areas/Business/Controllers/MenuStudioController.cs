using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Filters;
using DukkanPilot.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/MenuStudio")]
[RequireActiveSubscription]
public class MenuStudioController : BusinessBaseController
{
    private readonly AppDbContext _context;
    private readonly BusinessPlanLimitHelper _planLimitHelper;

    public MenuStudioController(AppDbContext context, BusinessPlanLimitHelper planLimitHelper)
    {
        _context = context;
        _planLimitHelper = planLimitHelper;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "menu-studio";

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

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.IsActive,
                ProductCount = c.Products.Count,
                ActiveProductCount = c.Products.Count(p => p.IsActive),
                AveragePrice = c.Products.Any() ? c.Products.Average(p => p.Price) : 0m
            })
            .OrderBy(c => c.Name)
            .ToListAsync();

        var productStats = await _context.Products
            .AsNoTracking()
            .Where(p => p.BusinessId == businessId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(p => p.IsActive),
                Average = g.Any() ? g.Average(p => p.Price) : 0m,
                Max = g.Any() ? g.Max(p => p.Price) : 0m,
                Min = g.Any() ? g.Min(p => p.Price) : 0m
            })
            .FirstOrDefaultAsync();

        var totalProducts = productStats?.Total ?? 0;
        var activeProducts = productStats?.Active ?? 0;
        var totalCategories = categories.Count;
        var activeCategories = categories.Count(c => c.IsActive);

        var whatsAppNumber = !string.IsNullOrWhiteSpace(business.Setting?.WhatsAppNumber)
            ? business.Setting.WhatsAppNumber
            : business.Phone;

        var publicMenuUrl = $"{Request.Scheme}://{Request.Host}/m/{business.Slug}";
        var isBusinessOwner = User.IsInRole(nameof(UserRole.BusinessOwner));
        var usage = await _planLimitHelper.GetUsageAsync(businessId);

        var hasBusinessName = !string.IsNullOrWhiteSpace(business.Name);
        var hasDescription = !string.IsNullOrWhiteSpace(business.Description);
        var hasLogo = !string.IsNullOrWhiteSpace(business.LogoUrl);
        var hasWhatsApp = !string.IsNullOrWhiteSpace(whatsAppNumber);
        var hasCategory = totalCategories > 0;
        var hasActiveProduct = activeProducts > 0;
        var hasPublicMenuLink = !string.IsNullOrWhiteSpace(business.Slug);

        var healthChecks = new List<MenuHealthCheckItemViewModel>
        {
            new()
            {
                Label = "İşletme adı",
                IsComplete = hasBusinessName,
                ActionLabel = isBusinessOwner ? "İşletme Ayarları" : null,
                ActionUrl = isBusinessOwner ? "/Business/Settings" : null
            },
            new()
            {
                Label = "İşletme açıklaması",
                IsComplete = hasDescription,
                ActionLabel = isBusinessOwner ? "İşletme Ayarları" : null,
                ActionUrl = isBusinessOwner ? "/Business/Settings" : null
            },
            new()
            {
                Label = "Logo",
                IsComplete = hasLogo,
                ActionLabel = isBusinessOwner ? "İşletme Ayarları" : null,
                ActionUrl = isBusinessOwner ? "/Business/Settings" : null
            },
            new()
            {
                Label = "WhatsApp numarası",
                IsComplete = hasWhatsApp,
                ActionLabel = isBusinessOwner ? "İşletme Ayarları" : null,
                ActionUrl = isBusinessOwner ? "/Business/Settings" : null
            },
            new()
            {
                Label = "En az 1 kategori",
                IsComplete = hasCategory,
                ActionLabel = "Kategori Ekle",
                ActionUrl = "/Business/Categories/Create"
            },
            new()
            {
                Label = "En az 1 aktif ürün",
                IsComplete = hasActiveProduct,
                ActionLabel = "Ürün Ekle",
                ActionUrl = "/Business/Products/Create"
            },
            new()
            {
                Label = "QR menü linki hazır",
                IsComplete = hasPublicMenuLink,
                ActionLabel = "QR Menü",
                ActionUrl = "/Business/QrMenu"
            },
            new()
            {
                Label = "QR kod / afiş aksiyonları",
                IsComplete = true,
                ActionLabel = "QR Afişi",
                ActionUrl = "/Business/QrMenu/Print"
            }
        };

        var model = new MenuStudioViewModel
        {
            BusinessName = business.Name,
            BusinessSlug = business.Slug,
            PublicMenuUrl = publicMenuUrl,
            LogoUrl = business.LogoUrl,
            Description = business.Description,
            ThemeColor = ResolveThemeColor(business.Setting?.ThemeColor),
            IsBusinessOwner = isBusinessOwner,
            HasBusinessName = hasBusinessName,
            HasBusinessDescription = hasDescription,
            HasLogo = hasLogo,
            HasWhatsAppNumber = hasWhatsApp,
            HasCategory = hasCategory,
            HasActiveProduct = hasActiveProduct,
            HasPublicMenuLink = hasPublicMenuLink,
            HasQrActions = true,
            TotalCategories = totalCategories,
            ActiveCategories = activeCategories,
            TotalProducts = totalProducts,
            ActiveProducts = activeProducts,
            PassiveProducts = totalProducts - activeProducts,
            AverageProductPrice = productStats?.Average ?? 0m,
            MaxProductPrice = productStats?.Max ?? 0m,
            MinProductPrice = productStats?.Min ?? 0m,
            ProductPlanUsage = usage.Products,
            HealthChecks = healthChecks,
            CategorySummaries = categories.Select(c => new MenuStudioCategorySummaryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive,
                ProductCount = c.ProductCount,
                ActiveProductCount = c.ActiveProductCount,
                AveragePrice = c.AveragePrice,
                IsPublicVisible = c.IsActive && c.ActiveProductCount > 0
            }).ToList()
        };

        return View(model);
    }

    private static string ResolveThemeColor(string? themeColor)
    {
        if (string.IsNullOrWhiteSpace(themeColor))
        {
            return "#2563eb";
        }

        var trimmed = themeColor.Trim();
        return trimmed.StartsWith('#') && trimmed.Length is 4 or 7 ? trimmed : "#2563eb";
    }
}
