using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

public class DashboardController : AdminBaseController
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "dashboard";

        var model = new AdminDashboardViewModel
        {
            TotalBusinessCount = await _context.Businesses.CountAsync(),
            ActiveBusinessCount = await _context.Businesses.CountAsync(b => b.IsActive),
            TotalPlanCount = await _context.SubscriptionPlans.CountAsync(),
            ActiveSubscriptionCount = await _context.BusinessSubscriptions.CountAsync(s =>
                s.IsActive && s.Status == SubscriptionStatus.Active),
            TotalProductCount = await _context.Products.CountAsync(),
            TotalOrderCount = await _context.Orders.CountAsync()
        };

        return View(model);
    }
}
