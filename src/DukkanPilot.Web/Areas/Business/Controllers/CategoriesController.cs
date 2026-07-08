using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Filters;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Categories")]
[RequireActiveSubscription]
public class CategoriesController : BusinessBaseController
{
    private readonly AppDbContext _context;
    private readonly BusinessPlanLimitHelper _planLimitHelper;
    private readonly IAuditLogService _auditLog;

    public CategoriesController(AppDbContext context, BusinessPlanLimitHelper planLimitHelper, IAuditLogService auditLog)
    {
        _context = context;
        _planLimitHelper = planLimitHelper;
        _auditLog = auditLog;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "categories";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryListViewModel
            {
                Id = c.Id,
                Name = c.Name,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                ProductCount = c.Products.Count,
                ActiveProductCount = c.Products.Count(p => p.IsActive),
                AveragePrice = c.Products.Any() ? c.Products.Average(p => p.Price) : 0m,
                IsPublicVisible = c.IsActive && c.Products.Any(p => p.IsActive)
            })
            .ToListAsync();

        var model = new CategoriesIndexViewModel
        {
            TotalCategories = categories.Count,
            ActiveCategories = categories.Count(c => c.IsActive),
            PassiveCategories = categories.Count(c => !c.IsActive),
            Categories = categories
        };

        return View(model);
    }

    [HttpPost("ToggleActive/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        if (category is null)
        {
            return NotFound();
        }

        category.IsActive = !category.IsActive;
        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = category.IsActive
            ? "Kategori aktif duruma alındı."
            : "Kategori pasif duruma alındı.";

        await _auditLog.LogBusinessAsync(
            businessId,
            "Category.StatusChanged",
            "Category",
            category.Id,
            category.IsActive ? $"Kategori aktif duruma alındı: {category.Name}" : $"Kategori pasif duruma alındı: {category.Name}",
            new { isActive = category.IsActive });

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        ViewData["ActiveMenu"] = "categories-create";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        await PlanLimitViewDataHelper.SetLimitWarningAsync(this, _planLimitHelper, businessId, PlanLimitResource.Categories);

        return View(new CategoryFormViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        ViewData["ActiveMenu"] = "categories-create";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (await _planLimitHelper.IsLimitReachedAsync(businessId, PlanLimitResource.Categories))
        {
            ModelState.AddModelError(string.Empty,
                _planLimitHelper.GetLimitReachedMessage(PlanLimitResource.Categories, User.IsInRole(nameof(UserRole.BusinessOwner))));
        }

        if (!await IsCategoryNameAvailableAsync(businessId, model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "Bu kategori adı zaten kullanılıyor.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var category = new Category
        {
            BusinessId = businessId,
            Name = model.Name.Trim(),
            SortOrder = model.SortOrder,
            IsActive = model.IsActive
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Kategori başarıyla oluşturuldu.";

        await _auditLog.LogBusinessAsync(
            businessId,
            "Category.Created",
            "Category",
            category.Id,
            $"Kategori oluşturuldu: {category.Name}");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveMenu"] = "categories";

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
        ViewData["ActiveMenu"] = "categories";

        var model = await BuildFormViewModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
    {
        ViewData["ActiveMenu"] = "categories";

        if (id != model.Id)
        {
            return BadRequest();
        }

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (!await IsCategoryNameAvailableAsync(businessId, model.Name, id))
        {
            ModelState.AddModelError(nameof(model.Name), "Bu kategori adı zaten kullanılıyor.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        if (category is null)
        {
            return NotFound();
        }

        category.Name = model.Name.Trim();
        category.SortOrder = model.SortOrder;
        category.IsActive = model.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Kategori başarıyla güncellendi.";

        await _auditLog.LogBusinessAsync(
            businessId,
            "Category.Updated",
            "Category",
            category.Id,
            $"Kategori güncellendi: {category.Name}");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        ViewData["ActiveMenu"] = "categories";

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

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        if (category is null)
        {
            return NotFound();
        }

        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Kategori pasif duruma alındı.";

        await _auditLog.LogBusinessAsync(
            businessId,
            "Category.Deleted",
            "Category",
            category.Id,
            $"Kategori silindi (pasif duruma alındı): {category.Name}");

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> IsCategoryNameAvailableAsync(int businessId, string name, int? excludeId = null)
    {
        var normalizedName = name.Trim();
        return !await _context.Categories.AnyAsync(c =>
            c.BusinessId == businessId &&
            c.Name == normalizedName &&
            (!excludeId.HasValue || c.Id != excludeId.Value));
    }

    private async Task<CategoryFormViewModel?> BuildFormViewModelAsync(int id)
    {
        var businessId = GetCurrentBusinessId();
        if (businessId is null)
        {
            return null;
        }

        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        if (category is null)
        {
            return null;
        }

        return new CategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive
        };
    }

    private async Task<CategoryDetailsViewModel?> BuildDetailsViewModelAsync(int id)
    {
        var businessId = GetCurrentBusinessId();
        if (businessId is null)
        {
            return null;
        }

        var category = await _context.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        if (category is null)
        {
            return null;
        }

        return new CategoryDetailsViewModel
        {
            Id = category.Id,
            Name = category.Name,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt,
            ProductCount = category.Products.Count,
            ActiveProductCount = category.Products.Count(p => p.IsActive)
        };
    }
}
