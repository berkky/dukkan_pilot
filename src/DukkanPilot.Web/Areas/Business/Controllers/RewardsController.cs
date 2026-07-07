using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using Microsoft.AspNetCore.Mvc;
using DukkanPilot.Web.Filters;
using DukkanPilot.Web.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Rewards")]
[RequireActiveSubscription]
public class RewardsController : BusinessBaseController
{
    private readonly AppDbContext _context;
    private readonly BusinessPlanLimitHelper _planLimitHelper;

    public RewardsController(AppDbContext context, BusinessPlanLimitHelper planLimitHelper)
    {
        _context = context;
        _planLimitHelper = planLimitHelper;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "rewards";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var items = await _context.Rewards
            .AsNoTracking()
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RewardListViewModel
            {
                Id = r.Id,
                Name = r.Name,
                RequiredPoints = r.RequiredPoints,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return View(items);
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        ViewData["ActiveMenu"] = "rewards-create";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        await PlanLimitViewDataHelper.SetLimitWarningAsync(this, _planLimitHelper, businessId, PlanLimitResource.Rewards);

        return View(new RewardFormViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RewardFormViewModel model)
    {
        ViewData["ActiveMenu"] = "rewards-create";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (await _planLimitHelper.IsLimitReachedAsync(businessId, PlanLimitResource.Rewards))
        {
            ModelState.AddModelError(string.Empty,
                _planLimitHelper.GetLimitReachedMessage(PlanLimitResource.Rewards, User.IsInRole(nameof(UserRole.BusinessOwner))));
        }

        if (!await IsRewardNameAvailableAsync(businessId, model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "Bu ödül adı zaten kullanılıyor.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var reward = new Reward
        {
            BusinessId = businessId,
            Name = model.Name.Trim(),
            Description = TrimToNull(model.Description),
            RequiredPoints = model.RequiredPoints,
            IsActive = model.IsActive
        };

        _context.Rewards.Add(reward);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Ödül başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveMenu"] = "rewards";

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
        ViewData["ActiveMenu"] = "rewards";

        var model = await BuildFormViewModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RewardFormViewModel model)
    {
        ViewData["ActiveMenu"] = "rewards";

        if (id != model.Id)
        {
            return BadRequest();
        }

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (!await IsRewardNameAvailableAsync(businessId, model.Name, id))
        {
            ModelState.AddModelError(nameof(model.Name), "Bu ödül adı zaten kullanılıyor.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var reward = await _context.Rewards
            .FirstOrDefaultAsync(r => r.Id == id && r.BusinessId == businessId);

        if (reward is null)
        {
            return NotFound();
        }

        reward.Name = model.Name.Trim();
        reward.Description = TrimToNull(model.Description);
        reward.RequiredPoints = model.RequiredPoints;
        reward.IsActive = model.IsActive;
        reward.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Ödül başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        ViewData["ActiveMenu"] = "rewards";

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

        var reward = await _context.Rewards
            .FirstOrDefaultAsync(r => r.Id == id && r.BusinessId == businessId);

        if (reward is null)
        {
            return NotFound();
        }

        reward.IsActive = false;
        reward.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Ödül pasif duruma alındı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Redeem/{id:int}")]
    public async Task<IActionResult> Redeem(int id)
    {
        ViewData["ActiveMenu"] = "rewards";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var reward = await _context.Rewards
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.BusinessId == businessId);

        if (reward is null)
        {
            return NotFound();
        }

        if (!reward.IsActive)
        {
            TempData["Error"] = "Pasif ödüller kullanılamaz.";
            return RedirectToAction(nameof(Index));
        }

        return View(new RewardRedeemViewModel
        {
            RewardId = reward.Id,
            RewardName = reward.Name,
            RequiredPoints = reward.RequiredPoints,
            AvailableCustomers = await GetCustomerSelectListAsync(businessId)
        });
    }

    [HttpPost("Redeem/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Redeem(int id, RewardRedeemViewModel model)
    {
        ViewData["ActiveMenu"] = "rewards";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (id != model.RewardId)
        {
            return BadRequest();
        }

        var reward = await _context.Rewards
            .FirstOrDefaultAsync(r => r.Id == id && r.BusinessId == businessId);

        if (reward is null)
        {
            return NotFound();
        }

        model.RewardName = reward.Name;
        model.RequiredPoints = reward.RequiredPoints;
        model.AvailableCustomers = await GetCustomerSelectListAsync(businessId);

        if (!reward.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Pasif ödüller kullanılamaz.");
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c =>
                c.Id == model.CustomerId &&
                c.BusinessId == businessId &&
                c.IsActive);

        if (customer is null)
        {
            ModelState.AddModelError(nameof(model.CustomerId), "Geçerli bir aktif müşteri seçin.");
        }
        else if (customer.TotalPoints < reward.RequiredPoints)
        {
            ModelState.AddModelError(
                nameof(model.CustomerId),
                $"Müşterinin puanı yetersiz. Mevcut: {customer.TotalPoints}, gerekli: {reward.RequiredPoints}.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (customer is null)
        {
            return View(model);
        }

        customer.TotalPoints -= reward.RequiredPoints;
        customer.UpdatedAt = DateTime.UtcNow;

        var description = TrimToNull(model.Description) ?? $"Ödül kullanıldı: {reward.Name}";

        var transaction = new LoyaltyTransaction
        {
            BusinessId = businessId,
            CustomerId = customer.Id,
            RewardId = reward.Id,
            Points = reward.RequiredPoints,
            Type = LoyaltyTransactionType.Redeem,
            Description = description
        };

        _context.LoyaltyTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Ödül kullanıldı: {reward.Name} ({reward.RequiredPoints} puan düşüldü).";
        return Redirect($"/Business/Customers/Details/{customer.Id}");
    }
    private async Task<bool> IsRewardNameAvailableAsync(int businessId, string name, int? excludeId = null)
    {
        var normalizedName = name.Trim();
        return !await _context.Rewards.AnyAsync(r =>
            r.BusinessId == businessId &&
            r.Name == normalizedName &&
            (!excludeId.HasValue || r.Id != excludeId.Value));
    }

    private async Task<RewardFormViewModel?> BuildFormViewModelAsync(int id)
    {
        var businessId = GetCurrentBusinessId();
        if (businessId is null)
        {
            return null;
        }

        var reward = await _context.Rewards
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.BusinessId == businessId);

        if (reward is null)
        {
            return null;
        }

        return new RewardFormViewModel
        {
            Id = reward.Id,
            Name = reward.Name,
            Description = reward.Description,
            RequiredPoints = reward.RequiredPoints,
            IsActive = reward.IsActive
        };
    }

    private async Task<RewardDetailsViewModel?> BuildDetailsViewModelAsync(int id)
    {
        var businessId = GetCurrentBusinessId();
        if (businessId is null)
        {
            return null;
        }

        var reward = await _context.Rewards
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.BusinessId == businessId);

        if (reward is null)
        {
            return null;
        }

        var recentRedemptions = await _context.LoyaltyTransactions
            .AsNoTracking()
            .Where(t =>
                t.BusinessId == businessId &&
                t.RewardId == reward.Id &&
                t.Type == LoyaltyTransactionType.Redeem)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => new RewardRedemptionHistoryViewModel
            {
                CreatedAt = t.CreatedAt,
                CustomerName = t.Customer.Name,
                CustomerPhone = t.Customer.Phone,
                Points = t.Points,
                Description = t.Description
            })
            .ToListAsync();

        return new RewardDetailsViewModel
        {
            Id = reward.Id,
            Name = reward.Name,
            Description = reward.Description,
            RequiredPoints = reward.RequiredPoints,
            IsActive = reward.IsActive,
            CreatedAt = reward.CreatedAt,
            RecentRedemptions = recentRedemptions
        };
    }

    private async Task<List<SelectListItem>> GetCustomerSelectListAsync(int businessId)
    {
        return await _context.Customers
            .AsNoTracking()
            .Where(c => c.BusinessId == businessId && c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.Name} ({c.Phone}) — {c.TotalPoints} puan"
            })
            .ToListAsync();
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
