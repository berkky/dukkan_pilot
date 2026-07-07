using DukkanPilot.Core.Entities;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Customers")]
[RequireActiveSubscription]
public class CustomersController : BusinessBaseController
{
    private readonly AppDbContext _context;

    public CustomersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "customers";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var items = await _context.Customers
            .AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CustomerListViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                TotalPoints = c.TotalPoints,
                IsActive = c.IsActive,
                OrderCount = _context.Orders.Count(o =>
                    o.BusinessId == businessId &&
                    (o.CustomerId == c.Id ||
                     (o.CustomerId == null && o.CustomerPhone != null && c.Phone != null && o.CustomerPhone == c.Phone))),
                LastOrderDate = _context.Orders
                    .Where(o =>
                        o.BusinessId == businessId &&
                        (o.CustomerId == c.Id ||
                         (o.CustomerId == null && o.CustomerPhone != null && c.Phone != null && o.CustomerPhone == c.Phone)))
                    .Max(o => (DateTime?)o.CreatedAt)
            })
            .ToListAsync();

        return View(items);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        ViewData["ActiveMenu"] = "customers-create";
        return View(new CustomerFormViewModel());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel model)
    {
        ViewData["ActiveMenu"] = "customers-create";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (!await IsPhoneAvailableAsync(businessId, model.Phone))
        {
            ModelState.AddModelError(nameof(model.Phone), "Bu telefon numarası bu işletmede zaten kayıtlı.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var customer = new Customer
        {
            BusinessId = businessId,
            Name = model.Name.Trim(),
            Phone = model.Phone.Trim(),
            Notes = TrimToNull(model.Notes),
            TotalPoints = model.TotalPoints,
            IsActive = model.IsActive
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Müşteri başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveMenu"] = "customers";

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
        ViewData["ActiveMenu"] = "customers";

        var model = await BuildFormViewModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerFormViewModel model)
    {
        ViewData["ActiveMenu"] = "customers";

        if (id != model.Id)
        {
            return BadRequest();
        }

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (!await IsPhoneAvailableAsync(businessId, model.Phone, id))
        {
            ModelState.AddModelError(nameof(model.Phone), "Bu telefon numarası bu işletmede zaten kayıtlı.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        if (customer is null)
        {
            return NotFound();
        }

        customer.Name = model.Name.Trim();
        customer.Phone = model.Phone.Trim();
        customer.Notes = TrimToNull(model.Notes);
        customer.TotalPoints = model.TotalPoints;
        customer.IsActive = model.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Müşteri başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        ViewData["ActiveMenu"] = "customers";

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

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        if (customer is null)
        {
            return NotFound();
        }

        customer.IsActive = false;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Müşteri pasif duruma alındı.";
        return RedirectToAction(nameof(Index));
    }
    private async Task<bool> IsPhoneAvailableAsync(int businessId, string phone, int? excludeCustomerId = null)
    {
        var normalizedPhone = phone.Trim();
        return !await _context.Customers.AnyAsync(c =>
            c.BusinessId == businessId &&
            c.Phone == normalizedPhone &&
            (!excludeCustomerId.HasValue || c.Id != excludeCustomerId.Value));
    }

    private static string? TrimToNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private async Task<CustomerFormViewModel?> BuildFormViewModelAsync(int id)
    {
        var businessId = GetCurrentBusinessId();
        if (businessId is null)
        {
            return null;
        }

        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        if (customer is null)
        {
            return null;
        }

        return new CustomerFormViewModel
        {
            Id = customer.Id,
            Name = customer.Name,
            Phone = customer.Phone ?? string.Empty,
            Notes = customer.Notes,
            TotalPoints = customer.TotalPoints,
            IsActive = customer.IsActive
        };
    }

    private async Task<CustomerDetailsViewModel?> BuildDetailsViewModelAsync(int id)
    {
        var businessId = GetCurrentBusinessId();
        if (businessId is null)
        {
            return null;
        }

        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

        if (customer is null)
        {
            return null;
        }

        var orderHistory = await _context.Orders
            .AsNoTracking()
            .Where(o =>
                o.BusinessId == businessId &&
                (o.CustomerId == customer.Id ||
                 (o.CustomerId == null && o.CustomerPhone != null && customer.Phone != null && o.CustomerPhone == customer.Phone)))
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new CustomerOrderHistoryViewModel
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                CreatedAt = o.CreatedAt,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                Source = o.Source
            })
            .ToListAsync();

        var loyaltyTransactions = await _context.LoyaltyTransactions
            .AsNoTracking()
            .Where(t => t.BusinessId == businessId && t.CustomerId == customer.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => new CustomerLoyaltyTransactionViewModel
            {
                CreatedAt = t.CreatedAt,
                Type = t.Type,
                Points = t.Points,
                Description = t.Description,
                RewardName = t.Reward != null ? t.Reward.Name : null
            })
            .ToListAsync();

        return new CustomerDetailsViewModel
        {
            Id = customer.Id,
            Name = customer.Name,
            Phone = customer.Phone,
            Notes = customer.Notes,
            TotalPoints = customer.TotalPoints,
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt,
            OrderHistory = orderHistory,
            LoyaltyTransactions = loyaltyTransactions
        };
    }
}
