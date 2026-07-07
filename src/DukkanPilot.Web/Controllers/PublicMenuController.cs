using System.Globalization;
using System.Text;
using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Models.PublicMenu;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Controllers;

public class PublicMenuController : Controller
{
    private readonly AppDbContext _context;

    public PublicMenuController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("/m/{slug}")]
    public async Task<IActionResult> Index(string slug)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        var business = await _context.Businesses
            .AsNoTracking()
            .Include(b => b.Setting)
            .FirstOrDefaultAsync(b => b.Slug == normalizedSlug && b.IsActive);

        if (business is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("NotFound", normalizedSlug);
        }

        var now = DateTime.UtcNow;

        var campaigns = await _context.Campaigns
            .AsNoTracking()
            .Where(c => c.BusinessId == business.Id
                && c.IsActive
                && c.StartDate <= now
                && (c.EndDate == null || c.EndDate >= now))
            .OrderByDescending(c => c.StartDate)
            .Select(c => new PublicMenuCampaignViewModel
            {
                Title = c.Title,
                Description = c.Description,
                StartDate = c.StartDate,
                EndDate = c.EndDate
            })
            .ToListAsync();

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.BusinessId == business.Id && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new PublicMenuCategoryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                SortOrder = c.SortOrder,
                Products = c.Products
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.SortOrder)
                    .ThenBy(p => p.Name)
                    .Select(p => new PublicMenuProductViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        ImageUrl = p.ImageUrl,
                        SortOrder = p.SortOrder
                    })
                    .ToList()
            })
            .ToListAsync();

        var model = new PublicMenuViewModel
        {
            BusinessId = business.Id,
            BusinessName = business.Name,
            Slug = business.Slug,
            Phone = business.Phone,
            LogoUrl = business.LogoUrl,
            ThemeColor = business.Setting?.ThemeColor ?? "#2563eb",
            Currency = business.Setting?.Currency ?? "TRY",
            WhatsAppNumber = business.Setting?.WhatsAppNumber,
            Campaigns = campaigns,
            Categories = categories
        };

        ViewData["Title"] = $"{business.Name} Menü | DukkanPilot";

        return View(model);
    }

    [HttpPost("/m/{slug}/order")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(string slug, [FromBody] PlaceOrderRequest request)
    {
        if (request.Items.Count == 0)
        {
            return BadRequest(new { error = "Sepetiniz boş." });
        }

        var normalizedSlug = slug.Trim().ToLowerInvariant();

        var business = await _context.Businesses
            .Include(b => b.Setting)
            .FirstOrDefaultAsync(b => b.Slug == normalizedSlug && b.IsActive);

        if (business is null)
        {
            return NotFound(new { error = "İşletme bulunamadı." });
        }

        var whatsAppNumber = NormalizeWhatsAppNumber(business.Setting?.WhatsAppNumber);
        if (whatsAppNumber is null)
        {
            return BadRequest(new { error = "İşletmenin WhatsApp numarası tanımlı değil." });
        }

        var requestedItems = request.Items
            .Where(i => i.Quantity > 0)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .ToList();

        if (requestedItems.Count == 0)
        {
            return BadRequest(new { error = "Geçerli ürün bulunamadı." });
        }

        var productIds = requestedItems.Select(i => i.ProductId).ToList();

        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.BusinessId == business.Id && p.IsActive && productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        if (products.Count != productIds.Count)
        {
            return BadRequest(new { error = "Sepetteki bazı ürünler artık mevcut değil." });
        }

        var orderItems = new List<OrderItem>();
        decimal totalAmount = 0m;
        var messageLines = new List<(string Name, int Quantity, decimal UnitPrice)>();

        foreach (var item in requestedItems)
        {
            var product = products[item.ProductId];
            var lineTotal = product.Price * item.Quantity;
            totalAmount += lineTotal;

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });

            messageLines.Add((product.Name, item.Quantity, product.Price));
        }

        var orderNumber = GenerateOrderNumber();

        var order = new Order
        {
            BusinessId = business.Id,
            CustomerId = null,
            OrderNumber = orderNumber,
            TotalAmount = totalAmount,
            Status = OrderStatus.Pending,
            Source = OrderSource.WhatsApp,
            Notes = TrimToMax(request.Notes, 1000),
            CustomerName = TrimToMax(request.CustomerName, 200),
            CustomerPhone = TrimToMax(request.CustomerPhone, 20)
        };

        foreach (var orderItem in orderItems)
        {
            order.Items.Add(orderItem);
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var message = BuildWhatsAppMessage(
            business.Name,
            orderNumber,
            messageLines,
            totalAmount,
            order.CustomerName,
            order.CustomerPhone,
            order.Notes);

        var whatsAppUrl = $"https://wa.me/{whatsAppNumber}?text={Uri.EscapeDataString(message)}";

        return Ok(new PlaceOrderResponse
        {
            OrderId = order.Id,
            OrderNumber = orderNumber,
            WhatsAppUrl = whatsAppUrl
        });
    }

    private static string GenerateOrderNumber()
    {
        var random = Random.Shared.Next(1000, 9999);
        return $"DP-{DateTime.UtcNow:yyyyMMddHHmmss}-{random}";
    }

    private static string? NormalizeWhatsAppNumber(string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            return null;
        }

        var digits = new string(number.Where(char.IsDigit).ToArray());
        return digits.Length > 0 ? digits : null;
    }

    private static string? TrimToMax(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string BuildWhatsAppMessage(
        string businessName,
        string orderNumber,
        IReadOnlyList<(string Name, int Quantity, decimal UnitPrice)> items,
        decimal total,
        string? customerName,
        string? customerPhone,
        string? notes)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var sb = new StringBuilder();

        sb.AppendLine($"Merhaba, {businessName} menüsünden sipariş vermek istiyorum.");
        sb.AppendLine();
        sb.AppendLine($"Sipariş No: {orderNumber}");

        if (!string.IsNullOrWhiteSpace(customerName))
        {
            sb.AppendLine($"Ad: {customerName}");
        }

        if (!string.IsNullOrWhiteSpace(customerPhone))
        {
            sb.AppendLine($"Telefon: {customerPhone}");
        }

        sb.AppendLine("---");

        foreach (var item in items)
        {
            var lineTotal = item.Quantity * item.UnitPrice;
            sb.AppendLine($"{item.Quantity}x {item.Name} — {lineTotal.ToString("N2", culture)} ₺");
        }

        sb.AppendLine("---");
        sb.AppendLine($"Toplam: {total.ToString("N2", culture)} ₺");

        if (!string.IsNullOrWhiteSpace(notes))
        {
            sb.AppendLine();
            sb.AppendLine($"Not: {notes}");
        }

        return sb.ToString().TrimEnd();
    }
}
