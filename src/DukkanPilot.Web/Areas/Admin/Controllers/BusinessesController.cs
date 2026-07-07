using BusinessEntity = DukkanPilot.Core.Entities.Business;
using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Admin.Models;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Route("Admin/Businesses")]
public class BusinessesController : AdminBaseController
{
    private readonly AppDbContext _context;

    public BusinessesController(AppDbContext context)
    {
        _context = context;
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
            .Include(b => b.Subscriptions)
                .ThenInclude(s => s.SubscriptionPlan)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var productCounts = await _context.Products
            .AsNoTracking()
            .GroupBy(p => p.BusinessId)
            .Select(g => new { BusinessId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BusinessId, x => x.Count);

        var orderStats = await _context.Orders
            .AsNoTracking()
            .GroupBy(o => o.BusinessId)
            .Select(g => new
            {
                BusinessId = g.Key,
                Count = g.Count(),
                Revenue = g.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount)
            })
            .ToDictionaryAsync(x => x.BusinessId, x => x);

        var filtered = allBusinesses.AsEnumerable();

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

        var businesses = filtered
            .Select(b =>
            {
                var latest = AdminSaasQueryHelper.GetLatestSubscription(b.Subscriptions);
                productCounts.TryGetValue(b.Id, out var productCount);
                orderStats.TryGetValue(b.Id, out var orders);

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
                    TotalRevenue = orders?.Revenue ?? 0m
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
            Businesses = businesses
        };

        return View(model);
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
}
