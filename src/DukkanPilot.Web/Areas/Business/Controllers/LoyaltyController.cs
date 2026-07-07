using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using Microsoft.AspNetCore.Mvc;
using DukkanPilot.Web.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Loyalty")]
[RequireActiveSubscription]
public class LoyaltyController : BusinessBaseController
{
    private const decimal DefaultMinimumOrderAmount = 10m;

    private readonly AppDbContext _context;

    public LoyaltyController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "loyalty";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var activeRule = await GetActiveRuleAsync(businessId);

        var model = new LoyaltyDashboardViewModel
        {
            HasActiveRule = activeRule is not null && activeRule.IsActive,
            PointsPerAmount = activeRule?.PointsPerAmount,
            MinimumOrderAmount = activeRule?.MinimumOrderAmount,
            RuleDescription = activeRule?.Description,
            ActiveCustomerCount = await _context.Customers.CountAsync(c =>
                c.BusinessId == businessId && c.IsActive),
            TotalEarnedPoints = await _context.LoyaltyTransactions
                .Where(t => t.BusinessId == businessId && t.Type == LoyaltyTransactionType.Earn)
                .SumAsync(t => t.Points),
            TotalRedeemedPoints = await _context.LoyaltyTransactions
                .Where(t => t.BusinessId == businessId && t.Type == LoyaltyTransactionType.Redeem)
                .SumAsync(t => t.Points),
            ActiveRewardCount = await _context.Rewards
                .CountAsync(r => r.BusinessId == businessId && r.IsActive),
            LowestActiveRewardName = await _context.Rewards
                .Where(r => r.BusinessId == businessId && r.IsActive)
                .OrderBy(r => r.RequiredPoints)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(),
            LowestActiveRewardPoints = await _context.Rewards
                .Where(r => r.BusinessId == businessId && r.IsActive)
                .OrderBy(r => r.RequiredPoints)
                .Select(r => (int?)r.RequiredPoints)
                .FirstOrDefaultAsync(),
            RecentTransactions = await BuildTransactionListAsync(businessId, 10)
        };

        return View(model);
    }

    [HttpGet("EditRule")]
    public async Task<IActionResult> EditRule()
    {
        ViewData["ActiveMenu"] = "loyalty";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var rule = await GetEditableRuleAsync(businessId);

        var model = rule is null
            ? new LoyaltyRuleFormViewModel()
            : new LoyaltyRuleFormViewModel
            {
                Id = rule.Id,
                PointsPerAmount = rule.PointsPerAmount,
                IsActive = rule.IsActive
            };

        return View(model);
    }

    [HttpPost("EditRule")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRule(LoyaltyRuleFormViewModel model)
    {
        ViewData["ActiveMenu"] = "loyalty";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var rule = await GetEditableRuleAsync(businessId);

        if (rule is null)
        {
            rule = new LoyaltyRule
            {
                BusinessId = businessId,
                MinimumOrderAmount = DefaultMinimumOrderAmount,
                Description = BuildRuleDescription(model.PointsPerAmount, DefaultMinimumOrderAmount)
            };
            _context.LoyaltyRules.Add(rule);
        }

        rule.PointsPerAmount = model.PointsPerAmount;
        rule.IsActive = model.IsActive;
        rule.Description = BuildRuleDescription(model.PointsPerAmount, rule.MinimumOrderAmount);
        rule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Sadakat kuralı kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Transactions")]
    public async Task<IActionResult> Transactions()
    {
        ViewData["ActiveMenu"] = "loyalty-transactions";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var items = await BuildTransactionListAsync(businessId);
        return View(items);
    }

    [HttpGet("AddTransaction")]
    public async Task<IActionResult> AddTransaction()
    {
        ViewData["ActiveMenu"] = "loyalty";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        return View(new LoyaltyTransactionFormViewModel
        {
            AvailableCustomers = await GetCustomerSelectListAsync(businessId)
        });
    }

    [HttpPost("AddTransaction")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTransaction(LoyaltyTransactionFormViewModel model)
    {
        ViewData["ActiveMenu"] = "loyalty";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        model.AvailableCustomers = await GetCustomerSelectListAsync(businessId);

        if (model.Type is not (LoyaltyTransactionType.Earn or LoyaltyTransactionType.Redeem))
        {
            ModelState.AddModelError(nameof(model.Type), "Geçersiz işlem tipi.");
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
        else if (model.Type == LoyaltyTransactionType.Redeem && customer.TotalPoints < model.Points)
        {
            ModelState.AddModelError(nameof(model.Points), "Müşterinin puanı bu işlem için yetersiz.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (customer is null)
        {
            return View(model);
        }

        if (model.Type == LoyaltyTransactionType.Earn)
        {
            customer.TotalPoints += model.Points;
        }
        else
        {
            customer.TotalPoints -= model.Points;
        }

        customer.UpdatedAt = DateTime.UtcNow;

        var transaction = new LoyaltyTransaction
        {
            BusinessId = businessId,
            CustomerId = customer.Id,
            Points = model.Points,
            Type = model.Type,
            Description = TrimToNull(model.Description)
        };

        _context.LoyaltyTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        TempData["Success"] = model.Type == LoyaltyTransactionType.Earn
            ? "Puan başarıyla eklendi."
            : "Puan başarıyla düşüldü.";

        return RedirectToAction(nameof(Transactions));
    }
    private async Task<LoyaltyRule?> GetActiveRuleAsync(int businessId)
    {
        return await _context.LoyaltyRules
            .AsNoTracking()
            .Where(r => r.BusinessId == businessId && r.IsActive)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();
    }

    private async Task<LoyaltyRule?> GetEditableRuleAsync(int businessId)
    {
        var activeRule = await _context.LoyaltyRules
            .Where(r => r.BusinessId == businessId && r.IsActive)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (activeRule is not null)
        {
            return activeRule;
        }

        return await _context.LoyaltyRules
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();
    }

    private async Task<List<LoyaltyTransactionListViewModel>> BuildTransactionListAsync(int businessId, int? take = null)
    {
        IQueryable<LoyaltyTransaction> query = _context.LoyaltyTransactions
            .AsNoTracking()
            .Where(t => t.BusinessId == businessId)
            .OrderByDescending(t => t.CreatedAt);

        if (take.HasValue)
        {
            query = query.Take(take.Value);
        }

        return await query
            .Select(t => new LoyaltyTransactionListViewModel
            {
                CreatedAt = t.CreatedAt,
                CustomerName = t.Customer.Name,
                CustomerPhone = t.Customer.Phone,
                Type = t.Type,
                Points = t.Points,
                Description = t.Description
            })
            .ToListAsync();
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

    private static string BuildRuleDescription(decimal pointsPerAmount, decimal minimumOrderAmount)
    {
        return $"Her {minimumOrderAmount:0.##} TL harcamada {pointsPerAmount:0.##} puan kazanılır";
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
