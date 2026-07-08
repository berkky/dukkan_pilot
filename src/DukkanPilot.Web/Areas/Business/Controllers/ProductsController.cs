using System.Globalization;
using System.Text;
using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using Microsoft.AspNetCore.Mvc;
using DukkanPilot.Web.Filters;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Products")]
[RequireActiveSubscription]
public class ProductsController : BusinessBaseController
{
    private readonly AppDbContext _context;
    private readonly BusinessPlanLimitHelper _planLimitHelper;
    private readonly IAuditLogService _auditLog;

    public ProductsController(AppDbContext context, BusinessPlanLimitHelper planLimitHelper, IAuditLogService auditLog)
    {
        _context = context;
        _planLimitHelper = planLimitHelper;
        _auditLog = auditLog;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        int? categoryId,
        string? status,
        string? search,
        decimal? minPrice,
        decimal? maxPrice)
    {
        ViewData["ActiveMenu"] = "products";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var statusFilter = string.IsNullOrWhiteSpace(status) ? "all" : status.Trim().ToLowerInvariant();
        var searchTerm = search?.Trim();

        var allProductsQuery = _context.Products.AsNoTracking().Where(p => p.BusinessId == businessId);
        var allProducts = await allProductsQuery
            .Select(p => new { p.IsActive, p.Price })
            .ToListAsync();

        var usage = await _planLimitHelper.GetUsageAsync(businessId);

        var filteredQuery = _context.Products
            .AsNoTracking()
            .Where(p => p.BusinessId == businessId);

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            filteredQuery = filteredQuery.Where(p => p.CategoryId == categoryId.Value);
        }

        filteredQuery = statusFilter switch
        {
            "active" => filteredQuery.Where(p => p.IsActive),
            "passive" => filteredQuery.Where(p => !p.IsActive),
            _ => filteredQuery
        };

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredQuery = filteredQuery.Where(p =>
                p.Name.Contains(searchTerm) ||
                (p.Description != null && p.Description.Contains(searchTerm)));
        }

        if (minPrice.HasValue)
        {
            filteredQuery = filteredQuery.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            filteredQuery = filteredQuery.Where(p => p.Price <= maxPrice.Value);
        }

        var products = await filteredQuery
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Select(p => new ProductIndexRowViewModel
            {
                Id = p.Id,
                Name = p.Name,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                CategoryIsActive = p.Category.IsActive,
                Price = p.Price,
                SortOrder = p.SortOrder,
                IsActive = p.IsActive,
                IsPublicVisible = p.IsActive && p.Category.IsActive,
                Description = p.Description,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        var exportParams = BuildFilterQueryString(categoryId, statusFilter, searchTerm, minPrice, maxPrice);

        var model = new ProductsIndexViewModel
        {
            TotalProducts = allProducts.Count,
            ActiveProducts = allProducts.Count(p => p.IsActive),
            PassiveProducts = allProducts.Count(p => !p.IsActive),
            AveragePrice = allProducts.Count > 0 ? allProducts.Average(p => p.Price) : 0m,
            ProductPlanUsage = usage.Products,
            CategoryFilter = categoryId,
            StatusFilter = statusFilter,
            Search = searchTerm,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            AvailableCategories = await GetAllCategorySelectListAsync(businessId),
            Products = products,
            ExportCsvUrl = $"/Business/Products/ExportCsv{exportParams}"
        };

        return View(model);
    }

    [HttpGet("ExportCsv")]
    public async Task<IActionResult> ExportCsv(
        int? categoryId,
        string? status,
        string? search,
        decimal? minPrice,
        decimal? maxPrice)
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var statusFilter = string.IsNullOrWhiteSpace(status) ? "all" : status.Trim().ToLowerInvariant();
        var searchTerm = search?.Trim();
        var culture = CultureInfo.GetCultureInfo("tr-TR");

        var query = _context.Products.AsNoTracking().Where(p => p.BusinessId == businessId);

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        query = statusFilter switch
        {
            "active" => query.Where(p => p.IsActive),
            "passive" => query.Where(p => !p.IsActive),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p =>
                p.Name.Contains(searchTerm) ||
                (p.Description != null && p.Description.Contains(searchTerm)));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        var products = await query
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Select(p => new
            {
                p.Name,
                CategoryName = p.Category.Name,
                p.Price,
                p.IsActive,
                p.Description,
                p.CreatedAt
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Ürün Adı,Kategori,Fiyat,Durum,Açıklama,Oluşturma Tarihi");

        foreach (var product in products)
        {
            sb.Append(CsvEscape(product.Name));
            sb.Append(',');
            sb.Append(CsvEscape(product.CategoryName));
            sb.Append(',');
            sb.Append(CsvEscape(product.Price.ToString("N2", culture)));
            sb.Append(',');
            sb.Append(CsvEscape(product.IsActive ? "Aktif" : "Pasif"));
            sb.Append(',');
            sb.Append(CsvEscape(product.Description));
            sb.Append(',');
            sb.Append(CsvEscape(product.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm", culture)));
            sb.AppendLine();
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var fileName = $"dukkanpilot-urunler-{DateTime.Now:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    [HttpGet("DownloadImportTemplate")]
    public IActionResult DownloadImportTemplate()
    {
        var bytes = ProductCsvImportHelper.BuildTemplateBytes();
        return File(bytes, "text/csv; charset=utf-8", "dukkanpilot-urun-sablonu.csv");
    }

    [HttpGet("ImportCsv")]
    public IActionResult ImportCsv()
    {
        ViewData["ActiveMenu"] = "products";

        return View(new ProductImportCsvPageViewModel());
    }

    [HttpPost("ImportCsv")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportCsv(IFormFile? file, string mode)
    {
        ViewData["ActiveMenu"] = "products";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var normalizedMode = string.IsNullOrWhiteSpace(mode) ? "preview" : mode.Trim().ToLowerInvariant();
        var isDryRun = normalizedMode is not ("import" or "apply");

        if (file is null || file.Length == 0)
        {
            return View(new ProductImportCsvPageViewModel
            {
                Mode = normalizedMode,
                Result = new ProductCsvImportResultViewModel
                {
                    IsDryRun = isDryRun,
                    HasFileError = true,
                    FileErrorMessage = "Lütfen bir CSV dosyası seçin."
                }
            });
        }

        if (file.Length > ProductCsvImportHelper.MaxFileSizeBytes)
        {
            return View(new ProductImportCsvPageViewModel
            {
                Mode = normalizedMode,
                Result = new ProductCsvImportResultViewModel
                {
                    IsDryRun = isDryRun,
                    HasFileError = true,
                    FileErrorMessage = "Dosya boyutu 1 MB sınırını aşıyor."
                }
            });
        }

        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return View(new ProductImportCsvPageViewModel
            {
                Mode = normalizedMode,
                Result = new ProductCsvImportResultViewModel
                {
                    IsDryRun = isDryRun,
                    HasFileError = true,
                    FileErrorMessage = "Yalnızca .csv dosyaları kabul edilir."
                }
            });
        }

        await using var stream = file.OpenReadStream();
        var parseOutcome = ProductCsvImportHelper.ParseFile(stream);
        if (!parseOutcome.IsSuccess)
        {
            return View(new ProductImportCsvPageViewModel
            {
                Mode = normalizedMode,
                Result = new ProductCsvImportResultViewModel
                {
                    IsDryRun = isDryRun,
                    HasFileError = true,
                    FileErrorMessage = parseOutcome.ErrorMessage
                }
            });
        }

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        var categoriesByName = categories.ToDictionary(
            c => c.Name.Trim().ToLowerInvariant(),
            c => c.Id);

        var existingNames = await _context.Products
            .AsNoTracking()
            .Where(p => p.BusinessId == businessId)
            .Select(p => p.Name)
            .ToListAsync();

        var existingNameSet = existingNames.ToHashSet(StringComparer.Ordinal);
        var maxImportable = await GetMaxImportableCountAsync(businessId);

        var importResult = ProductCsvImportHelper.ProcessRows(
            parseOutcome.Rows,
            categoriesByName,
            existingNameSet,
            maxImportable,
            applyImport: !isDryRun);

        if (!isDryRun && importResult.ProductsToCreate.Count > 0)
        {
            foreach (var product in importResult.ProductsToCreate)
            {
                product.BusinessId = businessId;
                _context.Products.Add(product);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{importResult.ImportedRows} ürün başarıyla içe aktarıldı.";

            await _auditLog.LogBusinessAsync(
                businessId,
                "Product.Imported",
                "Product",
                null,
                $"CSV içe aktarma tamamlandı: {importResult.ImportedRows} ürün eklendi.",
                new
                {
                    totalRows = importResult.TotalRows,
                    importedRows = importResult.ImportedRows,
                    errorRows = importResult.ErrorRows,
                    skippedByPlanLimitRows = importResult.SkippedByPlanLimitRows
                });
        }
        else if (isDryRun && importResult.ValidRows > 0)
        {
            TempData["Success"] = "Önizleme tamamlandı. Veritabanına yazılmadı.";
        }

        var resultViewModel = MapImportResult(importResult, isDryRun, maxImportable);

        return View(new ProductImportCsvPageViewModel
        {
            Mode = normalizedMode,
            Result = resultViewModel
        });
    }

    [HttpPost("BulkAction")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAction(
        int[]? selectedProductIds,
        string? actionType,
        decimal? percentValue,
        string? returnUrl)
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (selectedProductIds is null || selectedProductIds.Length == 0)
        {
            TempData["Error"] = "Lütfen en az bir ürün seçin.";
            return LocalRedirect(ValidateReturnUrl(returnUrl));
        }

        var normalizedAction = (actionType ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedAction))
        {
            TempData["Error"] = "Geçersiz toplu işlem.";
            return LocalRedirect(ValidateReturnUrl(returnUrl));
        }

        var distinctIds = selectedProductIds.Where(id => id > 0).Distinct().ToArray();
        var products = await _context.Products
            .Where(p => p.BusinessId == businessId && distinctIds.Contains(p.Id))
            .ToListAsync();

        if (products.Count == 0)
        {
            TempData["Error"] = "Seçilen ürünler bulunamadı.";
            return LocalRedirect(ValidateReturnUrl(returnUrl));
        }

        var updatedCount = 0;

        switch (normalizedAction)
        {
            case "activate":
                foreach (var product in products)
                {
                    if (!product.IsActive)
                    {
                        product.IsActive = true;
                        product.UpdatedAt = DateTime.UtcNow;
                        updatedCount++;
                    }
                }
                TempData["Success"] = $"{updatedCount} ürün aktif yapıldı.";
                break;

            case "deactivate":
                foreach (var product in products)
                {
                    if (product.IsActive)
                    {
                        product.IsActive = false;
                        product.UpdatedAt = DateTime.UtcNow;
                        updatedCount++;
                    }
                }
                TempData["Success"] = $"{updatedCount} ürün pasif yapıldı.";
                break;

            case "priceincreasepercent":
            case "pricedecreasepercent":
                if (!percentValue.HasValue || percentValue.Value < 0 || percentValue.Value > 100)
                {
                    TempData["Error"] = "Yüzde değeri 0 ile 100 arasında olmalıdır.";
                    return LocalRedirect(ValidateReturnUrl(returnUrl));
                }

                var factor = percentValue.Value / 100m;
                var isIncrease = normalizedAction == "priceincreasepercent";

                foreach (var product in products)
                {
                    var newPrice = isIncrease
                        ? product.Price * (1 + factor)
                        : product.Price * (1 - factor);

                    if (newPrice < 0)
                    {
                        newPrice = 0;
                    }

                    product.Price = Math.Round(newPrice, 2, MidpointRounding.AwayFromZero);
                    product.UpdatedAt = DateTime.UtcNow;
                    updatedCount++;
                }

                TempData["Success"] = isIncrease
                    ? $"{updatedCount} ürünün fiyatı %{percentValue.Value:N0} artırıldı."
                    : $"{updatedCount} ürünün fiyatı %{percentValue.Value:N0} azaltıldı.";
                break;

            default:
                TempData["Error"] = "Geçersiz toplu işlem türü.";
                return LocalRedirect(ValidateReturnUrl(returnUrl));
        }

        if (updatedCount > 0)
        {
            await _context.SaveChangesAsync();

            await _auditLog.LogBusinessAsync(
                businessId,
                "Product.BulkAction",
                "Product",
                null,
                $"Toplu işlem uygulandı: {normalizedAction} ({updatedCount} ürün etkilendi).",
                new
                {
                    actionType = normalizedAction,
                    updatedCount,
                    requestedCount = distinctIds.Length,
                    percentValue
                });
        }

        return LocalRedirect(ValidateReturnUrl(returnUrl));
    }

    [HttpPost("ToggleActive/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, string? returnUrl)
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

        product.IsActive = !product.IsActive;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = product.IsActive
            ? "Ürün aktif duruma alındı."
            : "Ürün pasif duruma alındı.";

        await _auditLog.LogBusinessAsync(
            businessId,
            "Product.StatusChanged",
            "Product",
            product.Id,
            product.IsActive ? $"Ürün aktif duruma alındı: {product.Name}" : $"Ürün pasif duruma alındı: {product.Name}",
            new { isActive = product.IsActive });

        return LocalRedirect(ValidateReturnUrl(returnUrl));
    }

    [HttpPost("UpdatePrice/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrice(int id, decimal price, string? returnUrl)
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (price < 0)
        {
            TempData["Error"] = "Fiyat sıfırdan küçük olamaz.";
            return LocalRedirect(ValidateReturnUrl(returnUrl));
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.BusinessId == businessId);

        if (product is null)
        {
            return NotFound();
        }

        var oldPrice = product.Price;
        product.Price = price;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Ürün fiyatı güncellendi.";

        await _auditLog.LogBusinessAsync(
            businessId,
            "Product.PriceUpdated",
            "Product",
            product.Id,
            $"Ürün fiyatı güncellendi: {product.Name}",
            new { oldPrice, newPrice = price });

        return LocalRedirect(ValidateReturnUrl(returnUrl));
    }

    [HttpPost("Duplicate/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duplicate(int id, string? returnUrl)
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (await _planLimitHelper.IsLimitReachedAsync(businessId, PlanLimitResource.Products))
        {
            TempData["Error"] = _planLimitHelper.GetLimitReachedMessage(
                PlanLimitResource.Products,
                User.IsInRole(nameof(UserRole.BusinessOwner)));
            return LocalRedirect(ValidateReturnUrl(returnUrl));
        }

        var source = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.BusinessId == businessId);

        if (source is null)
        {
            return NotFound();
        }

        var copyName = await GenerateDuplicateNameAsync(businessId, source.Name);

        var duplicate = new Product
        {
            BusinessId = businessId,
            CategoryId = source.CategoryId,
            Name = copyName,
            Description = source.Description,
            Price = source.Price,
            ImageUrl = source.ImageUrl,
            SizeOption = source.SizeOption,
            SortOrder = source.SortOrder,
            IsActive = source.IsActive
        };

        _context.Products.Add(duplicate);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Ürün kopyalandı: {copyName}";

        await _auditLog.LogBusinessAsync(
            businessId,
            "Product.Duplicated",
            "Product",
            duplicate.Id,
            $"Ürün kopyalandı: {source.Name} → {copyName}",
            new { sourceProductId = source.Id, duplicateProductId = duplicate.Id });

        return LocalRedirect(ValidateReturnUrl(returnUrl));
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

        await PlanLimitViewDataHelper.SetLimitWarningAsync(this, _planLimitHelper, businessId, PlanLimitResource.Products);

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

        if (await _planLimitHelper.IsLimitReachedAsync(businessId, PlanLimitResource.Products))
        {
            ModelState.AddModelError(string.Empty,
                _planLimitHelper.GetLimitReachedMessage(PlanLimitResource.Products, User.IsInRole(nameof(UserRole.BusinessOwner))));
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

        await _auditLog.LogBusinessAsync(
            businessId,
            "Product.Created",
            "Product",
            product.Id,
            $"Ürün oluşturuldu: {product.Name}");

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

        await _auditLog.LogBusinessAsync(
            businessId,
            "Product.Updated",
            "Product",
            product.Id,
            $"Ürün güncellendi: {product.Name}");

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

        await _auditLog.LogBusinessAsync(
            businessId,
            "Product.StatusChanged",
            "Product",
            product.Id,
            $"Ürün silindi (pasif duruma alındı): {product.Name}",
            new { isActive = product.IsActive });

        return RedirectToAction(nameof(Index));
    }

    private async Task<int> GetMaxImportableCountAsync(int businessId)
    {
        var usage = await _planLimitHelper.GetUsageAsync(businessId);
        if (!usage.HasValidSubscription)
        {
            return 0;
        }

        if (usage.Products.IsUnlimited)
        {
            return int.MaxValue;
        }

        return Math.Max(0, usage.Products.Limit - usage.Products.Used);
    }

    private static ProductCsvImportResultViewModel MapImportResult(
        ProductCsvImportResult result,
        bool isDryRun,
        int maxImportable)
    {
        return new ProductCsvImportResultViewModel
        {
            IsDryRun = isDryRun,
            TotalRows = result.TotalRows,
            ValidRows = result.ValidRows,
            ErrorRows = result.ErrorRows,
            ImportedRows = result.ImportedRows,
            SkippedRows = result.ErrorRows + result.SkippedByPlanLimitRows,
            SkippedByPlanLimitRows = result.SkippedByPlanLimitRows,
            RemainingImportSlots = maxImportable,
            PreviewRows = result.PreviewRows.Select(r => new ProductCsvImportRowResultViewModel
            {
                RowNumber = r.RowNumber,
                CategoryName = r.CategoryName,
                ProductName = r.ProductName,
                Description = r.Description,
                Price = r.Price,
                IsActive = r.IsActive,
                Status = r.Status,
                ErrorMessage = r.ErrorMessage
            }).ToList()
        };
    }

    private async Task<string> GenerateDuplicateNameAsync(int businessId, string sourceName)
    {
        var baseName = $"{sourceName.Trim()} (Kopya)";
        var candidate = baseName;
        var counter = 2;

        while (await _context.Products.AnyAsync(p => p.BusinessId == businessId && p.Name == candidate))
        {
            candidate = $"{sourceName.Trim()} (Kopya {counter})";
            counter++;
        }

        return candidate;
    }

    private static string BuildFilterQueryString(
        int? categoryId,
        string statusFilter,
        string? search,
        decimal? minPrice,
        decimal? maxPrice)
    {
        var parts = new List<string>();

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            parts.Add($"categoryId={categoryId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "all")
        {
            parts.Add($"status={Uri.EscapeDataString(statusFilter)}");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            parts.Add($"search={Uri.EscapeDataString(search)}");
        }

        if (minPrice.HasValue)
        {
            parts.Add($"minPrice={minPrice.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        if (maxPrice.HasValue)
        {
            parts.Add($"maxPrice={maxPrice.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }

    private static string ValidateReturnUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            returnUrl.StartsWith("/Business/Products", StringComparison.OrdinalIgnoreCase))
        {
            return returnUrl;
        }

        return "/Business/Products";
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
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

    private async Task<List<SelectListItem>> GetAllCategorySelectListAsync(int businessId)
    {
        return await _context.Categories
            .AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToListAsync();
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
