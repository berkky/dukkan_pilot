using BusinessEntity = DukkanPilot.Core.Entities.Business;
using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Admin.Models;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Route("Admin/Businesses")]
public class BusinessesController : AdminBaseController
{
    private readonly AppDbContext _context;
    private readonly BusinessPlanLimitHelper _planLimitHelper;
    private readonly CustomerSuccessHealthHelper _successHelper;
    private readonly IAuditLogService _auditLog;

    public BusinessesController(
        AppDbContext context,
        BusinessPlanLimitHelper planLimitHelper,
        CustomerSuccessHealthHelper successHelper,
        IAuditLogService auditLog)
    {
        _context = context;
        _planLimitHelper = planLimitHelper;
        _successHelper = successHelper;
        _auditLog = auditLog;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search,
        string statusFilter = "all",
        string subscriptionFilter = "all",
        int? planFilter = null)
    {
        ViewData["ActiveMenu"] = "businesses";

        var now = DateTime.UtcNow;

        var allBusinesses = await _context.Businesses
            .AsNoTracking()
            .Include(b => b.Setting)
            .Include(b => b.Subscriptions)
                .ThenInclude(s => s.SubscriptionPlan)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var productCounts = await _context.Products
            .AsNoTracking()
            .GroupBy(p => p.BusinessId)
            .Select(g => new { BusinessId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BusinessId, x => x.Count);

        var activeProductCounts = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .GroupBy(p => p.BusinessId)
            .Select(g => new { BusinessId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BusinessId, x => x.Count);

        var activeCategoryCounts = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .GroupBy(c => c.BusinessId)
            .Select(g => new { BusinessId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BusinessId, x => x.Count);

        var orderStats = await _context.Orders
            .AsNoTracking()
            .GroupBy(o => o.BusinessId)
            .Select(g => new BusinessOrderStats
            {
                BusinessId = g.Key,
                Count = g.Count(),
                Revenue = g.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount),
                LastOrderAt = g.Max(o => (DateTime?)o.CreatedAt)
            })
            .ToDictionaryAsync(x => x.BusinessId, x => x);

        var filtered = FilterBusinesses(allBusinesses, search, statusFilter, subscriptionFilter, planFilter, now);

        var businesses = filtered
            .Select(b =>
            {
                var latest = AdminSaasQueryHelper.GetLatestSubscription(b.Subscriptions);
                productCounts.TryGetValue(b.Id, out var productCount);
                activeProductCounts.TryGetValue(b.Id, out var activeProducts);
                activeCategoryCounts.TryGetValue(b.Id, out var activeCategories);
                orderStats.TryGetValue(b.Id, out var orders);

                var healthInput = AdminBusinessHealthHelper.CreateInput(
                    b,
                    latest,
                    activeCategories,
                    activeProducts,
                    orders?.LastOrderAt,
                    now);
                var health = AdminBusinessHealthHelper.Evaluate(healthInput);
                var primaryRisk = health.Risks.FirstOrDefault();

                return new BusinessListViewModel
                {
                    Id = b.Id,
                    Name = b.Name,
                    Slug = b.Slug,
                    Phone = b.Phone,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt,
                    PlanName = latest?.SubscriptionPlan?.Name ?? "-",
                    SubscriptionStatusText = latest is not null
                        ? AdminSaasQueryHelper.GetStatusLabel(latest.Status)
                        : "-",
                    SubscriptionStatusBadgeClass = latest is not null
                        ? AdminSaasQueryHelper.GetStatusBadgeClass(latest.Status)
                        : "bg-secondary",
                    SubscriptionEndDate = latest?.EndDate,
                    ProductCount = productCount,
                    OrderCount = orders?.Count ?? 0,
                    TotalRevenue = orders?.Revenue ?? 0m,
                    HealthScore = health.Score,
                    HealthLabel = health.Label,
                    HealthBadgeClass = health.BadgeClass,
                    HasRisks = health.Risks.Count > 0,
                    PrimaryRiskReason = primaryRisk?.Reason ?? "Risk yok",
                    PrimaryRiskBadgeClass = primaryRisk?.BadgeClass ?? "bg-success"
                };
            })
            .ToList();

        var model = new BusinessesIndexViewModel
        {
            TotalBusinesses = allBusinesses.Count,
            ActiveBusinesses = allBusinesses.Count(b => b.IsActive),
            PassiveBusinesses = allBusinesses.Count(b => !b.IsActive),
            ActiveSubscriptionBusinesses = allBusinesses.Count(b =>
            {
                var latest = AdminSaasQueryHelper.GetLatestSubscription(b.Subscriptions);
                return latest is not null && AdminSaasQueryHelper.IsSubscriptionValid(latest, now);
            }),
            ExpiredSubscriptionBusinesses = allBusinesses.Count(b =>
            {
                var latest = AdminSaasQueryHelper.GetLatestSubscription(b.Subscriptions);
                return AdminSaasQueryHelper.IsExpiredSubscription(latest, now);
            }),
            Search = search,
            StatusFilter = statusFilter,
            SubscriptionFilter = subscriptionFilter,
            PlanFilter = planFilter,
            AvailablePlans = await GetPlanSelectListAsync(planFilter),
            ExportCsvUrl = $"/Admin/Businesses/ExportCsv{BuildFilterQueryString(search, statusFilter, subscriptionFilter, planFilter)}",
            Businesses = businesses
        };

        return View(model);
    }

    [HttpGet("ExportCsv")]
    public async Task<IActionResult> ExportCsv(
        string? search,
        string statusFilter = "all",
        string subscriptionFilter = "all",
        int? planFilter = null)
    {
        var now = DateTime.UtcNow;
        var culture = CultureInfo.GetCultureInfo("tr-TR");

        var allBusinesses = await _context.Businesses
            .AsNoTracking()
            .Include(b => b.Setting)
            .Include(b => b.Subscriptions)
                .ThenInclude(s => s.SubscriptionPlan)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var productCounts = await _context.Products
            .AsNoTracking()
            .GroupBy(p => p.BusinessId)
            .Select(g => new { BusinessId = g.Key, Total = g.Count(), Active = g.Count(p => p.IsActive) })
            .ToDictionaryAsync(x => x.BusinessId, x => x);

        var activeCategoryCounts = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .GroupBy(c => c.BusinessId)
            .Select(g => new { BusinessId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BusinessId, x => x.Count);

        var orderStats = await _context.Orders
            .AsNoTracking()
            .GroupBy(o => o.BusinessId)
            .Select(g => new BusinessOrderStats
            {
                BusinessId = g.Key,
                Count = g.Count(),
                Revenue = g.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount),
                LastOrderAt = g.Max(o => (DateTime?)o.CreatedAt)
            })
            .ToDictionaryAsync(x => x.BusinessId, x => x);

        var filtered = FilterBusinesses(allBusinesses, search, statusFilter, subscriptionFilter, planFilter, now);

        var sb = new StringBuilder();
        sb.AppendLine("İşletme Adı,Slug,Telefon,Aktiflik,Plan,Abonelik Durumu,Abonelik Başlangıç,Abonelik Bitiş,Toplam Ürün,Aktif Ürün,Toplam Sipariş,Toplam Ciro,Sağlık Skoru,Riskler");

        foreach (var business in filtered)
        {
            var latest = AdminSaasQueryHelper.GetLatestSubscription(business.Subscriptions);
            productCounts.TryGetValue(business.Id, out var products);
            activeCategoryCounts.TryGetValue(business.Id, out var activeCategories);
            orderStats.TryGetValue(business.Id, out var orders);

            var health = AdminBusinessHealthHelper.Evaluate(AdminBusinessHealthHelper.CreateInput(
                business,
                latest,
                activeCategories,
                products?.Active ?? 0,
                orders?.LastOrderAt,
                now));

            var risks = health.Risks.Count > 0
                ? string.Join("; ", health.Risks.Select(r => r.Reason))
                : "Yok";

            sb.Append(CsvEscape(business.Name));
            sb.Append(',');
            sb.Append(CsvEscape(business.Slug));
            sb.Append(',');
            sb.Append(CsvEscape(business.Phone ?? string.Empty));
            sb.Append(',');
            sb.Append(CsvEscape(business.IsActive ? "Aktif" : "Pasif"));
            sb.Append(',');
            sb.Append(CsvEscape(latest?.SubscriptionPlan?.Name ?? "-"));
            sb.Append(',');
            sb.Append(CsvEscape(latest is not null ? AdminSaasQueryHelper.GetStatusLabel(latest.Status) : "-"));
            sb.Append(',');
            sb.Append(CsvEscape(latest?.StartDate.ToLocalTime().ToString("dd.MM.yyyy", culture) ?? string.Empty));
            sb.Append(',');
            sb.Append(CsvEscape(latest?.EndDate?.ToLocalTime().ToString("dd.MM.yyyy", culture) ?? string.Empty));
            sb.Append(',');
            sb.Append(products?.Total ?? 0);
            sb.Append(',');
            sb.Append(products?.Active ?? 0);
            sb.Append(',');
            sb.Append(orders?.Count ?? 0);
            sb.Append(',');
            sb.Append(CsvEscape((orders?.Revenue ?? 0m).ToString("N2", culture)));
            sb.Append(',');
            sb.Append(health.Score);
            sb.Append(',');
            sb.Append(CsvEscape(risks));
            sb.AppendLine();
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"dukkanpilot-isletmeler-{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpPost("ToggleActive/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var business = await _context.Businesses.FindAsync(id);
        if (business is null)
        {
            return NotFound();
        }

        business.IsActive = !business.IsActive;
        business.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = business.IsActive
            ? "İşletme aktif duruma alındı."
            : "İşletme pasif duruma alındı.";

        await _auditLog.LogAdminAsync(
            "Admin.Business.StatusChanged",
            "Business",
            business.Id,
            business.IsActive ? $"İşletme aktif duruma alındı: {business.Name}" : $"İşletme pasif duruma alındı: {business.Name}",
            new { isActive = business.IsActive },
            businessId: business.Id);

        return RedirectToAction(nameof(Index));
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

        await _auditLog.LogAdminAsync(
            "Admin.Business.Created",
            "Business",
            business.Id,
            $"İşletme oluşturuldu: {business.Name}",
            businessId: business.Id);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveMenu"] = "businesses";

        var model = await BuildAdminDetailsViewModelAsync(id);
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

        await _auditLog.LogAdminAsync(
            "Admin.Business.Updated",
            "Business",
            business.Id,
            $"İşletme güncellendi: {business.Name}",
            businessId: business.Id);

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

        await _auditLog.LogAdminAsync(
            "Admin.Business.Deleted",
            "Business",
            business.Id,
            $"İşletme silindi (pasif duruma alındı): {business.Name}",
            businessId: business.Id);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Subscription/{businessId:int}")]
    public async Task<IActionResult> Subscription(int businessId)
    {
        ViewData["ActiveMenu"] = "businesses";

        var model = await BuildSubscriptionEditViewModelAsync(businessId);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost("Subscription/{businessId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscription(int businessId, BusinessSubscriptionEditViewModel model)
    {
        ViewData["ActiveMenu"] = "businesses";

        if (businessId != model.BusinessId)
        {
            return BadRequest();
        }

        if (!Enum.IsDefined(model.Status))
        {
            ModelState.AddModelError(nameof(model.Status), "Geçersiz abonelik durumu.");
        }

        if (!await _context.SubscriptionPlans.AnyAsync(p => p.Id == model.SubscriptionPlanId && p.IsActive))
        {
            ModelState.AddModelError(nameof(model.SubscriptionPlanId), "Seçilen abonelik planı geçerli değil.");
        }

        if (!await _context.Businesses.AnyAsync(b => b.Id == businessId))
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateSubscriptionSelectListsAsync(model);
            return View(model);
        }

        var startDate = ToStartOfUtcDay(model.StartDate);
        var endDate = model.EndDate.HasValue ? ToEndOfUtcDay(model.EndDate.Value) : (DateTime?)null;

        BusinessSubscription? subscription = null;
        if (model.SubscriptionId.HasValue)
        {
            subscription = await _context.BusinessSubscriptions
                .FirstOrDefaultAsync(s => s.Id == model.SubscriptionId.Value && s.BusinessId == businessId);
        }

        if (subscription is null)
        {
            subscription = await GetLatestSubscriptionAsync(businessId);
        }

        if (subscription is null)
        {
            subscription = new BusinessSubscription
            {
                BusinessId = businessId,
                SubscriptionPlanId = model.SubscriptionPlanId,
                StartDate = startDate,
                EndDate = endDate,
                Status = model.Status,
                IsActive = model.IsActive
            };
            _context.BusinessSubscriptions.Add(subscription);
        }
        else
        {
            subscription.SubscriptionPlanId = model.SubscriptionPlanId;
            subscription.StartDate = startDate;
            subscription.EndDate = endDate;
            subscription.Status = model.Status;
            subscription.IsActive = model.IsActive;
            subscription.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Abonelik başarıyla güncellendi.";

        await _auditLog.LogAdminAsync(
            "Admin.Subscription.Updated",
            "BusinessSubscription",
            subscription.Id,
            $"Abonelik güncellendi (İşletme #{businessId}).",
            new
            {
                subscriptionPlanId = model.SubscriptionPlanId,
                status = model.Status.ToString(),
                startDate = startDate,
                endDate = endDate,
                isActive = model.IsActive
            },
            businessId: businessId);

        return RedirectToAction(nameof(Details), new { id = businessId });
    }

    private async Task<BusinessSubscriptionEditViewModel?> BuildSubscriptionEditViewModelAsync(int businessId)
    {
        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business is null)
        {
            return null;
        }

        var latestSubscription = await GetLatestSubscriptionTrackedAsync(businessId, asNoTracking: true);

        var model = new BusinessSubscriptionEditViewModel
        {
            BusinessId = business.Id,
            BusinessName = business.Name,
            BusinessSlug = business.Slug,
            CurrentPlanName = latestSubscription?.SubscriptionPlan?.Name ?? "-",
            CurrentStatusText = latestSubscription is not null
                ? SubscriptionDisplayHelper.GetStatusLabel(latestSubscription.Status)
                : "-"
        };

        if (latestSubscription is not null)
        {
            model.SubscriptionId = latestSubscription.Id;
            model.SubscriptionPlanId = latestSubscription.SubscriptionPlanId;
            model.Status = latestSubscription.Status;
            model.StartDate = latestSubscription.StartDate.Date;
            model.EndDate = latestSubscription.EndDate?.Date;
            model.IsActive = latestSubscription.IsActive;
        }
        else
        {
            var defaultPlanId = await _context.SubscriptionPlans
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            model.SubscriptionPlanId = defaultPlanId;
            model.Status = SubscriptionStatus.Active;
            model.StartDate = DateTime.UtcNow.Date;
            model.EndDate = DateTime.UtcNow.Date.AddDays(30);
            model.IsActive = true;
        }

        await PopulateSubscriptionSelectListsAsync(model);
        return model;
    }

    private async Task<BusinessSubscription?> GetLatestSubscriptionAsync(int businessId)
    {
        return await GetLatestSubscriptionTrackedAsync(businessId, asNoTracking: false);
    }

    private async Task<BusinessSubscription?> GetLatestSubscriptionTrackedAsync(int businessId, bool asNoTracking)
    {
        IQueryable<BusinessSubscription> query = _context.BusinessSubscriptions
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.BusinessId == businessId);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query
            .OrderByDescending(s => s.StartDate)
            .ThenByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    private async Task PopulateSubscriptionSelectListsAsync(BusinessSubscriptionEditViewModel model)
    {
        model.AvailablePlans = await GetPlanSelectListAsync(model.SubscriptionPlanId);
        model.AvailableStatuses = GetStatusSelectList(model.Status);
    }

    private static List<SelectListItem> GetStatusSelectList(SubscriptionStatus selectedStatus)
    {
        return Enum.GetValues<SubscriptionStatus>()
            .Select(status => new SelectListItem
            {
                Value = ((int)status).ToString(),
                Text = SubscriptionDisplayHelper.GetStatusLabel(status),
                Selected = status == selectedStatus
            })
            .ToList();
    }

    private static DateTime ToStartOfUtcDay(DateTime date)
    {
        return DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
    }

    private static DateTime ToEndOfUtcDay(DateTime date)
    {
        return DateTime.SpecifyKind(date.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
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

    private async Task<List<SelectListItem>> GetPlanSelectListAsync(int? selectedPlanId = null)
    {
        return await _context.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive || (selectedPlanId.HasValue && p.Id == selectedPlanId.Value))
            .OrderBy(p => p.SortOrder)
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Name,
                Selected = selectedPlanId.HasValue && p.Id == selectedPlanId.Value
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

    private async Task<AdminBusinessDetailsViewModel?> BuildAdminDetailsViewModelAsync(int id)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);
        var last7Start = todayStart.AddDays(-6);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

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

        var latestSubscription = AdminSaasQueryHelper.GetLatestSubscription(business.Subscriptions);
        var isValid = latestSubscription is not null
            && AdminSaasQueryHelper.IsSubscriptionValid(latestSubscription, now);

        var totalCategories = await _context.Categories.CountAsync(c => c.BusinessId == id);
        var activeCategories = await _context.Categories.CountAsync(c => c.BusinessId == id && c.IsActive);
        var totalProducts = await _context.Products.CountAsync(p => p.BusinessId == id);
        var activeProducts = await _context.Products.CountAsync(p => p.BusinessId == id && p.IsActive);
        var averageProductPrice = await _context.Products
            .AsNoTracking()
            .Where(p => p.BusinessId == id && p.IsActive)
            .Select(p => (decimal?)p.Price)
            .AverageAsync() ?? 0m;

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.BusinessId == id)
            .ToListAsync();

        var revenueOrders = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
        var totalRevenue = revenueOrders.Sum(o => o.TotalAmount);
        var lastOrderAt = orders.Count > 0 ? orders.Max(o => (DateTime?)o.CreatedAt) : null;

        var health = AdminBusinessHealthHelper.Evaluate(AdminBusinessHealthHelper.CreateInput(
            business,
            latestSubscription,
            activeCategories,
            activeProducts,
            lastOrderAt,
            now));

        var planUsage = await _planLimitHelper.GetUsageAsync(id);

        var recentOrders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.BusinessId == id)
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => new AdminBusinessRecentOrderViewModel
            {
                OrderNumber = o.OrderNumber,
                CreatedAt = o.CreatedAt,
                CustomerName = o.CustomerName ?? "-",
                TotalAmount = o.TotalAmount,
                Status = o.Status
            })
            .ToListAsync();

        foreach (var order in recentOrders)
        {
            order.StatusText = OrderDisplayHelper.GetStatusLabel(order.Status);
            order.StatusBadgeClass = OrderDisplayHelper.GetStatusBadgeClass(order.Status);
        }

        var topProducts = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order.BusinessId == id && oi.Order.Status != OrderStatus.Cancelled)
            .GroupBy(oi => oi.ProductName)
            .Select(g => new AdminBusinessTopProductViewModel
            {
                ProductName = g.Key,
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.UnitPrice * x.Quantity)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToListAsync();

        var ownerRole = await _context.UserBusinessRoles
            .AsNoTracking()
            .Include(r => r.AppUser)
            .Where(r => r.BusinessId == id && r.Role == BusinessRole.Owner && r.IsActive && r.AppUser.IsActive)
            .FirstOrDefaultAsync();

        var staffCount = await _context.UserBusinessRoles.CountAsync(r =>
            r.BusinessId == id &&
            r.Role == BusinessRole.Staff &&
            r.IsActive &&
            r.AppUser.IsActive);

        var totalRoleCount = await _context.UserBusinessRoles.CountAsync(r =>
            r.BusinessId == id && r.IsActive && r.AppUser.IsActive);

        int? daysRemaining = null;
        if (latestSubscription?.EndDate is not null)
        {
            daysRemaining = Math.Max(0, (int)Math.Ceiling((latestSubscription.EndDate.Value - now).TotalDays));
        }

        var revenueOrderCount = revenueOrders.Count;
        var averageBasket = revenueOrderCount > 0 ? totalRevenue / revenueOrderCount : 0m;
        var customerSuccess = await _successHelper.BuildAsync(id, $"/m/{business.Slug}", isBusinessOwner: true);

        return new AdminBusinessDetailsViewModel
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
            Health = health,
            Subscription = new AdminBusinessSubscriptionSummaryViewModel
            {
                PlanName = latestSubscription?.SubscriptionPlan?.Name ?? "-",
                StatusText = latestSubscription is not null
                    ? AdminSaasQueryHelper.GetStatusLabel(latestSubscription.Status)
                    : "-",
                StatusBadgeClass = latestSubscription is not null
                    ? AdminSaasQueryHelper.GetStatusBadgeClass(latestSubscription.Status)
                    : "bg-secondary",
                StartDate = latestSubscription?.StartDate,
                EndDate = latestSubscription?.EndDate,
                DaysRemaining = daysRemaining,
                PlanPrice = latestSubscription?.SubscriptionPlan?.Price ?? 0m,
                IsValid = isValid
            },
            PlanUsage = planUsage,
            CustomerSuccess = customerSuccess,
            Menu = new AdminBusinessMenuReadinessViewModel
            {
                TotalCategories = totalCategories,
                ActiveCategories = activeCategories,
                TotalProducts = totalProducts,
                ActiveProducts = activeProducts,
                PassiveProducts = totalProducts - activeProducts,
                AverageProductPrice = averageProductPrice,
                PublicMenuUrl = $"/m/{business.Slug}"
            },
            OrderSummary = new AdminBusinessOrderSummaryViewModel
            {
                TotalOrders = orders.Count,
                TotalRevenue = totalRevenue,
                TodayOrders = orders.Count(o => o.CreatedAt >= todayStart && o.CreatedAt < todayEnd),
                Last7DaysOrders = orders.Count(o => o.CreatedAt >= last7Start),
                ThisMonthRevenue = revenueOrders
                    .Where(o => o.CreatedAt >= monthStart)
                    .Sum(o => o.TotalAmount),
                AverageBasket = averageBasket,
                LastOrderAt = lastOrderAt
            },
            RecentOrders = recentOrders,
            TopProducts = topProducts,
            Users = new AdminBusinessUserSummaryViewModel
            {
                OwnerName = ownerRole?.AppUser.FullName,
                OwnerEmail = ownerRole?.AppUser.Email,
                StaffCount = staffCount,
                TotalRoleCount = totalRoleCount
            }
        };
    }

    private static IEnumerable<BusinessEntity> FilterBusinesses(
        IEnumerable<BusinessEntity> businesses,
        string? search,
        string statusFilter,
        string subscriptionFilter,
        int? planFilter,
        DateTime now)
    {
        var filtered = businesses;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            filtered = filtered.Where(b =>
                b.Name.ToLowerInvariant().Contains(term) ||
                b.Slug.ToLowerInvariant().Contains(term) ||
                (b.Phone?.ToLowerInvariant().Contains(term) ?? false));
        }

        filtered = statusFilter switch
        {
            "active" => filtered.Where(b => b.IsActive),
            "passive" => filtered.Where(b => !b.IsActive),
            _ => filtered
        };

        if (planFilter.HasValue)
        {
            filtered = filtered.Where(b =>
            {
                var latest = AdminSaasQueryHelper.GetLatestSubscription(b.Subscriptions);
                return latest?.SubscriptionPlanId == planFilter.Value;
            });
        }

        if (!string.Equals(subscriptionFilter, "all", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(b =>
            {
                var latest = AdminSaasQueryHelper.GetLatestSubscription(b.Subscriptions);
                return subscriptionFilter.ToLowerInvariant() switch
                {
                    "active" => latest is not null && AdminSaasQueryHelper.IsSubscriptionValid(latest, now),
                    "trial" => latest is not null
                        && latest.Status == SubscriptionStatus.Trial
                        && AdminSaasQueryHelper.IsSubscriptionValid(latest, now),
                    "expired" => AdminSaasQueryHelper.IsExpiredSubscription(latest, now),
                    "cancelled" => latest?.Status == SubscriptionStatus.Cancelled,
                    "none" => latest is null,
                    "expiring" => latest is not null && AdminSaasQueryHelper.IsExpiringSoon(latest, now),
                    _ => true
                };
            });
        }

        return filtered;
    }

    private static string BuildFilterQueryString(
        string? search,
        string statusFilter,
        string subscriptionFilter,
        int? planFilter)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            parts.Add($"search={Uri.EscapeDataString(search)}");
        }

        if (!string.Equals(statusFilter, "all", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add($"statusFilter={Uri.EscapeDataString(statusFilter)}");
        }

        if (!string.Equals(subscriptionFilter, "all", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add($"subscriptionFilter={Uri.EscapeDataString(subscriptionFilter)}");
        }

        if (planFilter.HasValue)
        {
            parts.Add($"planFilter={planFilter.Value}");
        }

        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private sealed class BusinessOrderStats
    {
        public int BusinessId { get; init; }

        public int Count { get; init; }

        public decimal Revenue { get; init; }

        public DateTime? LastOrderAt { get; init; }
    }
}
