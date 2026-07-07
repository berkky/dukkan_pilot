using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

public class DashboardController : BusinessBaseController
{
    private readonly AppDbContext _context;
    private readonly BusinessSubscriptionStatusHelper _subscriptionStatusHelper;

    public DashboardController(AppDbContext context, BusinessSubscriptionStatusHelper subscriptionStatusHelper)
    {
        _context = context;
        _subscriptionStatusHelper = subscriptionStatusHelper;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "dashboard";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business is null)
        {
            return NotFound();
        }

        var now = DateTime.UtcNow;
        var campaigns = _context.Campaigns.AsNoTracking().Where(c => c.BusinessId == businessId);

        var nearestEnding = await campaigns
            .Where(c => c.IsActive && c.EndDate != null && c.EndDate >= now)
            .OrderBy(c => c.EndDate)
            .Select(c => new { c.Title, c.EndDate })
            .FirstOrDefaultAsync();

        var model = new BusinessDashboardViewModel
        {
            BusinessName = business.Name,
            TotalCategoryCount = await _context.Categories.CountAsync(c => c.BusinessId == businessId),
            ActiveCategoryCount = await _context.Categories.CountAsync(c => c.BusinessId == businessId && c.IsActive),
            TotalProductCount = await _context.Products.CountAsync(p => p.BusinessId == businessId),
            ActiveProductCount = await _context.Products.CountAsync(p => p.BusinessId == businessId && p.IsActive),
            TotalOrderCount = await _context.Orders.CountAsync(o => o.BusinessId == businessId),
            TotalCustomerCount = await _context.Customers.CountAsync(c => c.BusinessId == businessId),
            ActiveCustomerCount = await _context.Customers.CountAsync(c => c.BusinessId == businessId && c.IsActive),
            RecentOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.BusinessId == businessId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new RecentOrderViewModel
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.CustomerName,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync(),
            LoyaltySummary = new LoyaltySummaryViewModel
            {
                TotalActiveCustomerPoints = await _context.Customers
                    .Where(c => c.BusinessId == businessId && c.IsActive)
                    .SumAsync(c => c.TotalPoints),
                LastTransactionDate = await _context.LoyaltyTransactions
                    .Where(t => t.BusinessId == businessId)
                    .MaxAsync(t => (DateTime?)t.CreatedAt),
                HasActiveLoyaltyRule = await _context.LoyaltyRules
                    .AnyAsync(r => r.BusinessId == businessId && r.IsActive)
            },
            CampaignSummary = new CampaignDashboardSummaryViewModel
            {
                TotalCampaignCount = await campaigns.CountAsync(),
                ActiveCampaignCount = await campaigns.CountAsync(c => c.IsActive),
                PublishedCampaignCount = await campaigns.CountAsync(c =>
                    c.IsActive &&
                    c.StartDate <= now &&
                    (c.EndDate == null || c.EndDate >= now)),
                NearestEndingCampaignTitle = nearestEnding?.Title,
                NearestEndingCampaignEndDate = nearestEnding?.EndDate
            },
            Subscription = await _subscriptionStatusHelper.GetStatusAsync(businessId),
            IsBusinessOwner = User.IsInRole(nameof(UserRole.BusinessOwner))
        };

        return View(model);
    }
}
