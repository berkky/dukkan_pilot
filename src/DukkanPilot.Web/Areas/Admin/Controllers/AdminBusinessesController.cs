using BusinessEntity = DukkanPilot.Core.Entities.Business;
using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Route("Admin/Businesses")]
public class AdminBusinessesController : AdminBaseController
{
    private readonly AppDbContext _context;

    public AdminBusinessesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "businesses";

        var items = await _context.Businesses
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BusinessListViewModel
            {
                Id = b.Id,
                Name = b.Name,
                Slug = b.Slug,
                Phone = b.Phone,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt,
                PlanName = b.Subscriptions
                    .Where(s => s.IsActive && s.Status == SubscriptionStatus.Active)
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => s.SubscriptionPlan.Name)
                    .FirstOrDefault() ?? "-"
            })
            .ToListAsync();

        return View(items);
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        ViewData["ActiveMenu"] = "businesses-create";
        return View(await BuildFormViewModelAsync());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BusinessFormViewModel model)
    {
        ViewData["ActiveMenu"] = "businesses-create";

        if (!await IsSlugAvailableAsync(model.Slug))
        {
            ModelState.AddModelError(nameof(model.Slug), "Bu slug zaten kullanılıyor.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailablePlans = await GetPlanSelectListAsync();
            return View(model);
        }

        var business = new BusinessEntity
        {
            Name = model.Name.Trim(),
            Slug = model.Slug.Trim().ToLowerInvariant(),
            Phone = model.Phone?.Trim(),
            LogoUrl = model.LogoUrl?.Trim(),
            IsActive = model.IsActive,
            Setting = new BusinessSetting
            {
                WhatsAppNumber = model.WhatsAppNumber?.Trim(),
                ThemeColor = model.ThemeColor.Trim(),
                Currency = model.Currency.Trim().ToUpperInvariant()
            },
            Subscriptions =
            {
                new BusinessSubscription
                {
                    SubscriptionPlanId = model.SubscriptionPlanId,
                    StartDate = DateTime.UtcNow,
                    Status = SubscriptionStatus.Active,
                    IsActive = true
                }
            }
        };

        _context.Businesses.Add(business);
        await _context.SaveChangesAsync();

        TempData["Success"] = "İşletme başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveMenu"] = "businesses";

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
        ViewData["ActiveMenu"] = "businesses";

        var model = await BuildFormViewModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BusinessFormViewModel model)
    {
        ViewData["ActiveMenu"] = "businesses";

        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!await IsSlugAvailableAsync(model.Slug, id))
        {
            ModelState.AddModelError(nameof(model.Slug), "Bu slug zaten kullanılıyor.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailablePlans = await GetPlanSelectListAsync();
            return View(model);
        }

        var business = await _context.Businesses
            .Include(b => b.Setting)
            .Include(b => b.Subscriptions)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (business is null)
        {
            return NotFound();
        }

        business.Name = model.Name.Trim();
        business.Slug = model.Slug.Trim().ToLowerInvariant();
        business.Phone = model.Phone?.Trim();
        business.LogoUrl = model.LogoUrl?.Trim();
        business.IsActive = model.IsActive;
        business.UpdatedAt = DateTime.UtcNow;

        if (business.Setting is null)
        {
            business.Setting = new BusinessSetting { BusinessId = business.Id };
        }

        business.Setting.WhatsAppNumber = model.WhatsAppNumber?.Trim();
        business.Setting.ThemeColor = model.ThemeColor.Trim();
        business.Setting.Currency = model.Currency.Trim().ToUpperInvariant();
        business.Setting.UpdatedAt = DateTime.UtcNow;

        await UpdateActiveSubscriptionAsync(business, model.SubscriptionPlanId);

        await _context.SaveChangesAsync();

        TempData["Success"] = "İşletme başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        ViewData["ActiveMenu"] = "businesses";

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
        var business = await _context.Businesses.FindAsync(id);
        if (business is null)
        {
            return NotFound();
        }

        business.IsActive = false;
        business.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "İşletme pasif duruma alındı.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<BusinessFormViewModel?> BuildFormViewModelAsync(int? id = null)
    {
        if (id is null)
        {
            return new BusinessFormViewModel
            {
                AvailablePlans = await GetPlanSelectListAsync()
            };
        }

        var business = await _context.Businesses
            .AsNoTracking()
            .Include(b => b.Setting)
            .Include(b => b.Subscriptions)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (business is null)
        {
            return null;
        }

        var activeSubscription = business.Subscriptions
            .Where(s => s.IsActive && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();

        return new BusinessFormViewModel
        {
            Id = business.Id,
            Name = business.Name,
            Slug = business.Slug,
            Phone = business.Phone,
            LogoUrl = business.LogoUrl,
            IsActive = business.IsActive,
            WhatsAppNumber = business.Setting?.WhatsAppNumber,
            ThemeColor = business.Setting?.ThemeColor ?? "#2563eb",
            Currency = business.Setting?.Currency ?? "TRY",
            SubscriptionPlanId = activeSubscription?.SubscriptionPlanId ?? 0,
            AvailablePlans = await GetPlanSelectListAsync()
        };
    }

    private async Task<BusinessDetailsViewModel?> BuildDetailsViewModelAsync(int id)
    {
        var business = await _context.Businesses
            .AsNoTracking()
            .Include(b => b.Setting)
            .Include(b => b.Subscriptions)
                .ThenInclude(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (business is null)
        {
            return null;
        }

        var activeSubscription = business.Subscriptions
            .Where(s => s.IsActive && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();

        var categoryCount = await _context.Categories.CountAsync(c => c.BusinessId == id);
        var productCount = await _context.Products.CountAsync(p => p.BusinessId == id);
        var customerCount = await _context.Customers.CountAsync(c => c.BusinessId == id);
        var orderCount = await _context.Orders.CountAsync(o => o.BusinessId == id);

        return new BusinessDetailsViewModel
        {
            Id = business.Id,
            Name = business.Name,
            Slug = business.Slug,
            Phone = business.Phone,
            Address = business.Address,
            LogoUrl = business.LogoUrl,
            Description = business.Description,
            IsActive = business.IsActive,
            CreatedAt = business.CreatedAt,
            UpdatedAt = business.UpdatedAt,
            WhatsAppNumber = business.Setting?.WhatsAppNumber,
            ThemeColor = business.Setting?.ThemeColor ?? "-",
            Currency = business.Setting?.Currency ?? "-",
            ActivePlanName = activeSubscription?.SubscriptionPlan.Name ?? "-",
            SubscriptionStatus = activeSubscription?.Status.ToString() ?? "-",
            SubscriptionStartDate = activeSubscription?.StartDate,
            SubscriptionEndDate = activeSubscription?.EndDate,
            CategoryCount = categoryCount,
            ProductCount = productCount,
            CustomerCount = customerCount,
            OrderCount = orderCount
        };
    }

    private async Task<List<SelectListItem>> GetPlanSelectListAsync()
    {
        return await _context.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Name
            })
            .ToListAsync();
    }

    private async Task<bool> IsSlugAvailableAsync(string slug, int? excludeId = null)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return !await _context.Businesses.AnyAsync(b =>
            b.Slug == normalizedSlug && (!excludeId.HasValue || b.Id != excludeId.Value));
    }

    private async Task UpdateActiveSubscriptionAsync(BusinessEntity business, int subscriptionPlanId)
    {
        var activeSubscription = business.Subscriptions
            .Where(s => s.IsActive && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();

        if (activeSubscription is null)
        {
            business.Subscriptions.Add(new BusinessSubscription
            {
                SubscriptionPlanId = subscriptionPlanId,
                StartDate = DateTime.UtcNow,
                Status = SubscriptionStatus.Active,
                IsActive = true
            });
            return;
        }

        if (activeSubscription.SubscriptionPlanId != subscriptionPlanId)
        {
            activeSubscription.SubscriptionPlanId = subscriptionPlanId;
            activeSubscription.UpdatedAt = DateTime.UtcNow;
        }
    }
}
