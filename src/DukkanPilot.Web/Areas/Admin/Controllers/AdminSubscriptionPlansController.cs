using DukkanPilot.Core.Entities;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Route("Admin/SubscriptionPlans")]
public class AdminSubscriptionPlansController : AdminBaseController
{
    private readonly AppDbContext _context;

    public AdminSubscriptionPlansController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "plans";

        var plans = await _context.SubscriptionPlans
            .AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Select(p => new SubscriptionPlanFormViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                MaxProducts = p.MaxProducts,
                MaxCampaigns = p.MaxCampaigns,
                IsActive = p.IsActive
            })
            .ToListAsync();

        return View(plans);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        ViewData["ActiveMenu"] = "plans-create";
        return View(new SubscriptionPlanFormViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubscriptionPlanFormViewModel model)
    {
        ViewData["ActiveMenu"] = "plans-create";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var maxSortOrder = await _context.SubscriptionPlans.MaxAsync(p => (int?)p.SortOrder) ?? 0;

        var plan = new SubscriptionPlan
        {
            Name = model.Name.Trim(),
            Description = model.Description?.Trim(),
            Price = model.Price,
            MaxProducts = model.MaxProducts,
            MaxCampaigns = model.MaxCampaigns,
            IsActive = model.IsActive,
            SortOrder = maxSortOrder + 1
        };

        _context.SubscriptionPlans.Add(plan);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Abonelik planı başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveMenu"] = "plans";

        var model = await GetPlanViewModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["ActiveMenu"] = "plans";

        var model = await GetPlanViewModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SubscriptionPlanFormViewModel model)
    {
        ViewData["ActiveMenu"] = "plans";

        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var plan = await _context.SubscriptionPlans.FindAsync(id);
        if (plan is null)
        {
            return NotFound();
        }

        plan.Name = model.Name.Trim();
        plan.Description = model.Description?.Trim();
        plan.Price = model.Price;
        plan.MaxProducts = model.MaxProducts;
        plan.MaxCampaigns = model.MaxCampaigns;
        plan.IsActive = model.IsActive;
        plan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Abonelik planı başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        ViewData["ActiveMenu"] = "plans";

        var model = await GetPlanViewModelAsync(id);
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
        var plan = await _context.SubscriptionPlans.FindAsync(id);
        if (plan is null)
        {
            return NotFound();
        }

        plan.IsActive = false;
        plan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Abonelik planı pasif duruma alındı.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<SubscriptionPlanFormViewModel?> GetPlanViewModelAsync(int id)
    {
        var plan = await _context.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan is null)
        {
            return null;
        }

        return new SubscriptionPlanFormViewModel
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            MaxProducts = plan.MaxProducts,
            MaxCampaigns = plan.MaxCampaigns,
            IsActive = plan.IsActive
        };
    }
}
