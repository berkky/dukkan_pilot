using System.Globalization;
using System.Text;
using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Filters;
using DukkanPilot.Web.Helpers;
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
    public async Task<IActionResult> Index(
        string? search,
        string? segment,
        string? lastOrder,
        decimal? minSpend,
        decimal? maxSpend)
    {
        ViewData["ActiveMenu"] = "customers";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var model = await BuildIndexViewModelAsync(
            businessId,
            search,
            segment,
            lastOrder,
            minSpend,
            maxSpend);

        return View(model);
    }

    [HttpGet("Insights")]
    public async Task<IActionResult> Insights()
    {
        ViewData["ActiveMenu"] = "customers-insights";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var utcNow = DateTime.UtcNow;
        var allStats = await LoadCustomerStatsAsync(businessId, utcNow);
        var total = allStats.Count;

        var segmentGroups = new[]
        {
            CustomerSegment.Vip,
            CustomerSegment.Returning,
            CustomerSegment.New,
            CustomerSegment.AtRisk,
            CustomerSegment.NoOrders
        };

        var distribution = segmentGroups.Select(seg => new CustomerSegmentSummaryViewModel
        {
            Segment = seg,
            Label = CustomerCrmHelper.GetSegmentLabel(seg),
            Count = CountBySegment(allStats, seg, utcNow),
            Percent = total > 0 ? (int)Math.Round(CountBySegment(allStats, seg, utcNow) * 100.0 / total) : 0,
            BadgeClass = CustomerCrmHelper.GetSegmentBadgeClass(seg)
        }).ToList();

        var model = new CustomerInsightsViewModel
        {
            SegmentDistribution = distribution,
            TopValuableCustomers = allStats
                .Where(s => s.TotalSpent > 0)
                .OrderByDescending(s => s.TotalSpent)
                .Take(10)
                .Select(MapToRow)
                .ToList(),
            TopFrequentCustomers = allStats
                .Where(s => s.OrderCount > 0)
                .OrderByDescending(s => s.OrderCount)
                .ThenByDescending(s => s.LastOrderDate)
                .Take(10)
                .Select(MapToRow)
                .ToList(),
            AtRiskCustomers = allStats
                .Where(s => CustomerCrmHelper.IsAtRisk(s, utcNow))
                .OrderBy(s => s.LastOrderDate)
                .Take(10)
                .Select(MapToRow)
                .ToList(),
            NewCustomers = allStats
                .Where(s => CustomerCrmHelper.IsNewCustomer(s, utcNow))
                .OrderByDescending(s => s.FirstOrderDate ?? s.CreatedAt)
                .Take(10)
                .Select(MapToRow)
                .ToList()
        };

        return View(model);
    }

    [HttpGet("ExportCsv")]
    public async Task<IActionResult> ExportCsv(
        string? search,
        string? segment,
        string? lastOrder,
        decimal? minSpend,
        decimal? maxSpend)
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var model = await BuildIndexViewModelAsync(
            businessId,
            search,
            segment,
            lastOrder,
            minSpend,
            maxSpend);

        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var sb = new StringBuilder();
        sb.AppendLine("Müşteri Adı,Telefon,Toplam Sipariş,Toplam Harcama,Ortalama Sepet,Son Sipariş Tarihi,Segment,Sadakat Puanı");

        foreach (var customer in model.Customers)
        {
            sb.Append(CsvEscape(customer.Name));
            sb.Append(',');
            sb.Append(CsvEscape(customer.Phone));
            sb.Append(',');
            sb.Append(customer.OrderCount);
            sb.Append(',');
            sb.Append(CsvEscape(customer.TotalSpent.ToString("N2", culture)));
            sb.Append(',');
            sb.Append(CsvEscape(customer.AverageBasket.ToString("N2", culture)));
            sb.Append(',');
            sb.Append(CsvEscape(customer.LastOrderDate.HasValue
                ? customer.LastOrderDate.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm", culture)
                : string.Empty));
            sb.Append(',');
            sb.Append(CsvEscape(customer.SegmentLabel));
            sb.Append(',');
            sb.Append(customer.TotalPoints);
            sb.AppendLine();
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"dukkanpilot-musteriler-{DateTime.Now:yyyyMMdd}.csv");
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

    private async Task<CustomersIndexViewModel> BuildIndexViewModelAsync(
        int businessId,
        string? search,
        string? segment,
        string? lastOrder,
        decimal? minSpend,
        decimal? maxSpend)
    {
        var utcNow = DateTime.UtcNow;
        var allStats = await LoadCustomerStatsAsync(businessId, utcNow);
        var searchTerm = search?.Trim();
        var segmentFilter = string.IsNullOrWhiteSpace(segment) ? "all" : segment.Trim().ToLowerInvariant();
        var lastOrderFilter = string.IsNullOrWhiteSpace(lastOrder) ? "all" : lastOrder.Trim().ToLowerInvariant();

        var monthStart = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var customersWithRevenue = allStats.Where(s => s.TotalSpent > 0).ToList();

        var filtered = allStats.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filtered = filtered.Where(s =>
                s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(s.Phone) && s.Phone.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(s.Email) && s.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        filtered = filtered.Where(s => CustomerCrmHelper.MatchesSegmentFilter(segmentFilter, s, utcNow));
        filtered = filtered.Where(s => CustomerCrmHelper.MatchesLastOrderFilter(lastOrderFilter, s, utcNow));

        if (minSpend.HasValue)
        {
            filtered = filtered.Where(s => s.TotalSpent >= minSpend.Value);
        }

        if (maxSpend.HasValue)
        {
            filtered = filtered.Where(s => s.TotalSpent <= maxSpend.Value);
        }

        var filteredList = filtered
            .OrderByDescending(s => s.LastOrderDate)
            .ThenByDescending(s => s.CreatedAt)
            .Select(MapToRow)
            .ToList();

        return new CustomersIndexViewModel
        {
            TotalCustomers = allStats.Count,
            NewCustomersThisMonth = allStats.Count(s =>
                s.CreatedAt >= monthStart ||
                (s.FirstOrderDate.HasValue && s.FirstOrderDate.Value >= monthStart)),
            ReturningCustomers = allStats.Count(s => CustomerCrmHelper.IsReturning(s)),
            VipCustomers = allStats.Count(s => CustomerCrmHelper.IsVip(s)),
            AtRiskCustomers = allStats.Count(s => CustomerCrmHelper.IsAtRisk(s, utcNow)),
            TotalCustomerRevenue = customersWithRevenue.Sum(s => s.TotalSpent),
            AverageCustomerSpend = customersWithRevenue.Count > 0
                ? customersWithRevenue.Average(s => s.TotalSpent)
                : 0m,
            Search = searchTerm,
            SegmentFilter = segmentFilter,
            LastOrderFilter = lastOrderFilter,
            MinSpend = minSpend,
            MaxSpend = maxSpend,
            ExportCsvUrl = $"/Business/Customers/ExportCsv{BuildFilterQueryString(searchTerm, segmentFilter, lastOrderFilter, minSpend, maxSpend)}",
            Customers = filteredList
        };
    }

    private async Task<List<CustomerCrmStats>> LoadCustomerStatsAsync(int businessId, DateTime utcNow)
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .ToListAsync();

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.BusinessId == businessId)
            .ToListAsync();

        return CustomerCrmStatsBuilder.Build(customers, orders, utcNow);
    }

    private static CustomerRowViewModel MapToRow(CustomerCrmStats stats) => new()
    {
        Id = stats.Id,
        Name = stats.Name,
        Phone = stats.Phone,
        TotalPoints = stats.TotalPoints,
        IsActive = stats.IsActive,
        OrderCount = stats.OrderCount,
        TotalSpent = stats.TotalSpent,
        AverageBasket = stats.AverageBasket,
        LastOrderDate = stats.LastOrderDate,
        Segment = stats.Segment,
        SegmentLabel = CustomerCrmHelper.GetSegmentLabel(stats.Segment),
        SegmentBadgeClass = CustomerCrmHelper.GetSegmentBadgeClass(stats.Segment),
        WhatsAppContactUrl = stats.WhatsAppContactUrl
    };

    private static int CountBySegment(List<CustomerCrmStats> stats, string segment, DateTime utcNow) =>
        segment switch
        {
            CustomerSegment.Vip => stats.Count(s => CustomerCrmHelper.IsVip(s)),
            CustomerSegment.Returning => stats.Count(s => CustomerCrmHelper.IsReturning(s)),
            CustomerSegment.New => stats.Count(s => CustomerCrmHelper.IsNewCustomer(s, utcNow)),
            CustomerSegment.AtRisk => stats.Count(s => CustomerCrmHelper.IsAtRisk(s, utcNow)),
            CustomerSegment.NoOrders => stats.Count(s => s.OrderCount == 0),
            _ => 0
        };

    private static string BuildFilterQueryString(
        string? search,
        string segmentFilter,
        string lastOrderFilter,
        decimal? minSpend,
        decimal? maxSpend)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            parts.Add($"search={Uri.EscapeDataString(search)}");
        }

        if (!string.IsNullOrWhiteSpace(segmentFilter) && segmentFilter != "all")
        {
            parts.Add($"segment={Uri.EscapeDataString(segmentFilter)}");
        }

        if (!string.IsNullOrWhiteSpace(lastOrderFilter) && lastOrderFilter != "all")
        {
            parts.Add($"lastOrder={Uri.EscapeDataString(lastOrderFilter)}");
        }

        if (minSpend.HasValue)
        {
            parts.Add($"minSpend={minSpend.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        if (maxSpend.HasValue)
        {
            parts.Add($"maxSpend={maxSpend.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
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

        var utcNow = DateTime.UtcNow;
        var stats = CustomerCrmStatsBuilder.Build([customer], await _context.Orders
            .AsNoTracking()
            .Where(o => o.BusinessId == businessId)
            .ToListAsync(), utcNow).First();

        var orderHistory = await _context.Orders
            .AsNoTracking()
            .Where(o =>
                o.BusinessId == businessId &&
                (o.CustomerId == customer.Id ||
                 (o.CustomerId == null && o.CustomerPhone != null && customer.Phone != null && o.CustomerPhone == customer.Phone)))
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
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

        var earnedPoints = await _context.LoyaltyTransactions
            .AsNoTracking()
            .Where(t => t.BusinessId == businessId && t.CustomerId == customer.Id && t.Type == LoyaltyTransactionType.Earn)
            .SumAsync(t => t.Points);

        var redeemedPoints = await _context.LoyaltyTransactions
            .AsNoTracking()
            .Where(t => t.BusinessId == businessId && t.CustomerId == customer.Id && t.Type == LoyaltyTransactionType.Redeem)
            .SumAsync(t => t.Points);

        var topProducts = await _context.OrderItems
            .AsNoTracking()
            .Where(i =>
                i.Order.BusinessId == businessId &&
                i.Order.Status != OrderStatus.Cancelled &&
                (i.Order.CustomerId == customer.Id ||
                 (i.Order.CustomerId == null && i.Order.CustomerPhone != null && customer.Phone != null && i.Order.CustomerPhone == customer.Phone)))
            .GroupBy(i => i.ProductName)
            .Select(g => new CustomerTopProductViewModel
            {
                ProductName = g.Key,
                Quantity = g.Sum(x => x.Quantity),
                TotalSpent = g.Sum(x => x.UnitPrice * x.Quantity)
            })
            .OrderByDescending(p => p.Quantity)
            .ThenByDescending(p => p.TotalSpent)
            .Take(5)
            .ToListAsync();

        return new CustomerDetailsViewModel
        {
            Id = customer.Id,
            Name = customer.Name,
            Phone = customer.Phone,
            Email = customer.Email,
            Notes = customer.Notes,
            TotalPoints = customer.TotalPoints,
            EarnedPoints = earnedPoints,
            RedeemedPoints = redeemedPoints,
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt,
            Segment = stats.Segment,
            SegmentLabel = CustomerCrmHelper.GetSegmentLabel(stats.Segment),
            SegmentBadgeClass = CustomerCrmHelper.GetSegmentBadgeClass(stats.Segment),
            WhatsAppContactUrl = stats.WhatsAppContactUrl,
            TotalOrders = stats.OrderCount,
            TotalSpent = stats.TotalSpent,
            AverageBasket = stats.AverageBasket,
            LastOrderDate = stats.LastOrderDate,
            FirstOrderDate = stats.FirstOrderDate,
            MaxOrderAmount = stats.MaxOrderAmount,
            Last30DaysOrderCount = stats.Last30DaysOrderCount,
            TopProductName = topProducts.FirstOrDefault()?.ProductName,
            OrderHistory = orderHistory,
            LoyaltyTransactions = loyaltyTransactions,
            TopProducts = topProducts
        };
    }
}
