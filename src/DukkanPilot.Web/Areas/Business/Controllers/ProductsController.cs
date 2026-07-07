using DukkanPilot.Core.Entities;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using Microsoft.AspNetCore.Mvc;
using DukkanPilot.Web.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Products")]
[RequireActiveSubscription]
public class ProductsController : BusinessBaseController
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "products";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var items = await _context.Products
            .AsNoTracking()
            .Where(p => p.BusinessId == businessId)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Select(p => new ProductListViewModel
            {
                Id = p.Id,
                Name = p.Name,
                CategoryName = p.Category.Name,
                Price = p.Price,
                SortOrder = p.SortOrder,
                IsActive = p.IsActive
            })
            .ToListAsync();

        return View(items);
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        ViewData["ActiveMenu"] = "products-create";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        return View(new ProductFormViewModel
        {
            AvailableCategories = await GetCategorySelectListAsync(businessId)
        });
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        ViewData["ActiveMenu"] = "products-create";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (!await IsCategoryValidAsync(businessId, model.CategoryId))
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Geçersiz kategori seçimi.");
        }

        if (!await IsProductNameAvailableAsync(businessId, model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "Bu ürün adı zaten kullanılıyor.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableCategories = await GetCategorySelectListAsync(businessId, model.CategoryId);
            return View(model);
        }

        var product = new Product
        {
            BusinessId = businessId,
            CategoryId = model.CategoryId,
            Name = model.Name.Trim(),
            Description = model.Description?.Trim(),
            Price = model.Price,
            ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim(),
            SortOrder = model.SortOrder,
            IsActive = model.IsActive
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Ürün başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveMenu"] = "products";

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
        ViewData["ActiveMenu"] = "products";

        var model = await BuildFormViewModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
    {
        ViewData["ActiveMenu"] = "products";

        if (id != model.Id)
        {
            return BadRequest();
        }

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (!await IsCategoryValidAsync(businessId, model.CategoryId, id))
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Geçersiz kategori seçimi.");
        }

        if (!await IsProductNameAvailableAsync(businessId, model.Name, id))
        {
            ModelState.AddModelError(nameof(model.Name), "Bu ürün adı zaten kullanılıyor.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableCategories = await GetCategorySelectListAsync(businessId, model.CategoryId);
            return View(model);
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.BusinessId == businessId);

        if (product is null)
        {
            return NotFound();
        }

        product.CategoryId = model.CategoryId;
        product.Name = model.Name.Trim();
        product.Description = model.Description?.Trim();
        product.Price = model.Price;
        product.ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim();
        product.SortOrder = model.SortOrder;
        product.IsActive = model.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Ürün başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        ViewData["ActiveMenu"] = "products";

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

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.BusinessId == businessId);

        if (product is null)
        {
            return NotFound();
        }

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Ürün pasif duruma alındı.";
        return RedirectToAction(nameof(Index));
    }
    private async Task<bool> IsCategoryValidAsync(int businessId, int categoryId, int? editingProductId = null)
    {
        if (editingProductId.HasValue)
        {
            var currentCategoryId = await _context.Products
                .AsNoTracking()
                .Where(p => p.Id == editingProductId.Value && p.BusinessId == businessId)
                .Select(p => (int?)p.CategoryId)
                .FirstOrDefaultAsync();

            if (currentCategoryId == categoryId)
            {
                return await _context.Categories.AnyAsync(c =>
                    c.Id == categoryId && c.BusinessId == businessId);
            }
        }

        return await _context.Categories.AnyAsync(c =>
            c.Id == categoryId && c.BusinessId == businessId && c.IsActive);
    }

    private async Task<bool> IsProductNameAvailableAsync(int businessId, string name, int? excludeId = null)
    {
        var normalizedName = name.Trim();
        return !await _context.Products.AnyAsync(p =>
            p.BusinessId == businessId &&
            p.Name == normalizedName &&
            (!excludeId.HasValue || p.Id != excludeId.Value));
    }

    private async Task<List<SelectListItem>> GetCategorySelectListAsync(int businessId, int? includeCategoryId = null)
    {
        return await _context.Categories
            .AsNoTracking()
            .Where(c => c.BusinessId == businessId && (c.IsActive || c.Id == includeCategoryId))
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToListAsync();
    }

    private async Task<ProductFormViewModel?> BuildFormViewModelAsync(int id)
    {
        var businessId = GetCurrentBusinessId();
        if (businessId is null)
        {
            return null;
        }

        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.BusinessId == businessId);

        if (product is null)
        {
            return null;
        }

        return new ProductFormViewModel
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            ImageUrl = product.ImageUrl,
            SortOrder = product.SortOrder,
            IsActive = product.IsActive,
            AvailableCategories = await GetCategorySelectListAsync(businessId.Value, product.CategoryId)
        };
    }

    private async Task<ProductDetailsViewModel?> BuildDetailsViewModelAsync(int id)
    {
        var businessId = GetCurrentBusinessId();
        if (businessId is null)
        {
            return null;
        }

        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.BusinessId == businessId);

        if (product is null)
        {
            return null;
        }

        return new ProductDetailsViewModel
        {
            Id = product.Id,
            Name = product.Name,
            CategoryName = product.Category.Name,
            Description = product.Description,
            Price = product.Price,
            ImageUrl = product.ImageUrl,
            SortOrder = product.SortOrder,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
