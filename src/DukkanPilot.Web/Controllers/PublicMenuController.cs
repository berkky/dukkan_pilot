using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Models.PublicMenu;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Controllers;

public class PublicMenuController : Controller
{
    private readonly AppDbContext _context;
    private readonly PublicOrderTrackingTokenHelper _trackingTokenHelper;

    public PublicMenuController(AppDbContext context, PublicOrderTrackingTokenHelper trackingTokenHelper)
    {
        _context = context;
        _trackingTokenHelper = trackingTokenHelper;
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

        var currency = ResolveCurrency(business.Setting?.Currency);
        var themeColor = ResolveThemeColor(business.Setting?.ThemeColor);
        var whatsAppNumber = ResolveWhatsAppNumber(business.Setting?.WhatsAppNumber, business.Phone);

        var model = new PublicMenuViewModel
        {
            BusinessId = business.Id,
            BusinessName = business.Name,
            Slug = business.Slug,
            Phone = business.Phone,
            LogoUrl = business.LogoUrl,
            Address = business.Address,
            Description = business.Description,
            ThemeColor = themeColor,
            Currency = currency,
            WhatsAppNumber = whatsAppNumber,
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

        var whatsAppNumber = ResolveWhatsAppNumber(business.Setting?.WhatsAppNumber, business.Phone);
        if (whatsAppNumber is null)
        {
            return BadRequest(new { error = "İşletmenin WhatsApp numarası tanımlı değil." });
        }

        var currency = ResolveCurrency(business.Setting?.Currency);

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
            currency,
            order.CustomerName,
            order.CustomerPhone,
            order.Notes);

        var whatsAppUrl = $"https://wa.me/{whatsAppNumber}?text={Uri.EscapeDataString(message)}";
        var trackingToken = _trackingTokenHelper.CreateToken(order.Id, business.Id, order.CreatedAt);
        var trackingUrl = BuildTrackingUrl(normalizedSlug, trackingToken);
        var confirmationUrl = BuildConfirmationUrl(normalizedSlug, trackingToken);

        return Ok(new PlaceOrderResponse
        {
            OrderId = order.Id,
            OrderNumber = orderNumber,
            WhatsAppUrl = whatsAppUrl,
            ConfirmationUrl = confirmationUrl,
            TrackingUrl = trackingUrl
        });
    }

    [HttpGet("/m/{slug}/order-confirmation/{token}")]
    public Task<IActionResult> OrderConfirmation(string slug, string token)
        => RenderOrderStatusPageAsync(slug, token, isConfirmationPage: true);

    [HttpGet("/m/{slug}/order-status/{token}")]
    public Task<IActionResult> TrackOrder(string slug, string token)
        => RenderOrderStatusPageAsync(slug, token, isConfirmationPage: false);

    [HttpGet("/m/{slug}/order-status/{token}/summary")]
    [ResponseCache(NoStore = true, Duration = 0)]
    public async Task<IActionResult> OrderStatusSummary(string slug, string token)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var validation = _trackingTokenHelper.TryValidateToken(token);
        if (!validation.IsValid || validation.Payload is null)
        {
            return NotFound();
        }

        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Slug == normalizedSlug && b.IsActive);

        if (business is null || business.Id != validation.Payload.BusinessId)
        {
            return NotFound();
        }

        var order = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == validation.Payload.OrderId && o.BusinessId == business.Id);

        if (order is null)
        {
            return NotFound();
        }

        return Json(new PublicOrderStatusSummaryResponse
        {
            Status = order.Status.ToString(),
            StatusText = PublicOrderDisplayHelper.GetStatusLabel(order.Status),
            StatusBadgeClass = PublicOrderDisplayHelper.GetStatusBadgeClass(order.Status),
            StatusMessage = PublicOrderDisplayHelper.GetStatusMessage(order.Status),
            TimelineSteps = PublicOrderDisplayHelper.GetTimelineSteps(order.Status).ToList(),
            TotalAmount = order.TotalAmount,
            UpdatedAt = order.UpdatedAt ?? order.CreatedAt,
            ServerTime = DateTime.UtcNow,
            IsCompleted = order.Status == OrderStatus.Completed,
            IsCancelled = order.Status == OrderStatus.Cancelled
        });
    }

    private async Task<IActionResult> RenderOrderStatusPageAsync(string slug, string token, bool isConfirmationPage)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var validation = _trackingTokenHelper.TryValidateToken(token);
        if (!validation.IsValid || validation.Payload is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("OrderTrackingNotFound", new PublicOrderTrackingNotFoundViewModel
            {
                BusinessSlug = normalizedSlug,
                ErrorMessage = validation.ErrorMessage ?? "Sipariş takip bağlantısı geçersiz.",
                IsExpired = validation.IsExpired
            });
        }

        var business = await _context.Businesses
            .AsNoTracking()
            .Include(b => b.Setting)
            .FirstOrDefaultAsync(b => b.Slug == normalizedSlug && b.IsActive);

        if (business is null || business.Id != validation.Payload.BusinessId)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("OrderTrackingNotFound", new PublicOrderTrackingNotFoundViewModel
            {
                BusinessSlug = normalizedSlug,
                ErrorMessage = "Sipariş takip bağlantısı geçersiz."
            });
        }

        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == validation.Payload.OrderId && o.BusinessId == business.Id);

        if (order is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("OrderTrackingNotFound", new PublicOrderTrackingNotFoundViewModel
            {
                BusinessSlug = normalizedSlug,
                ErrorMessage = "Sipariş bulunamadı."
            });
        }

        var currency = ResolveCurrency(business.Setting?.Currency);
        var themeColor = ResolveThemeColor(business.Setting?.ThemeColor);
        var whatsAppNumber = ResolveWhatsAppNumber(business.Setting?.WhatsAppNumber, business.Phone);
        var messageLines = order.Items
            .OrderBy(i => i.Id)
            .Select(i => (i.ProductName, i.Quantity, i.UnitPrice))
            .ToList();

        string? whatsAppUrl = null;
        if (whatsAppNumber is not null)
        {
            var message = BuildWhatsAppMessage(
                business.Name,
                order.OrderNumber,
                messageLines,
                order.TotalAmount,
                currency,
                order.CustomerName,
                order.CustomerPhone,
                order.Notes);
            whatsAppUrl = $"https://wa.me/{whatsAppNumber}?text={Uri.EscapeDataString(message)}";
        }

        var trackingUrl = BuildTrackingUrl(normalizedSlug, token);
        var model = new PublicOrderStatusViewModel
        {
            IsConfirmationPage = isConfirmationPage,
            BusinessName = business.Name,
            BusinessSlug = business.Slug,
            OrderNumber = order.OrderNumber,
            CustomerName = order.CustomerName,
            CreatedAt = order.CreatedAt,
            TotalAmount = order.TotalAmount,
            Currency = currency,
            Status = order.Status,
            Items = order.Items
                .OrderBy(i => i.Id)
                .Select(i => new PublicOrderStatusItemViewModel
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                })
                .ToList(),
            WhatsAppUrl = whatsAppUrl,
            TrackingUrl = trackingUrl,
            PublicMenuUrl = $"/m/{normalizedSlug}",
            SummaryUrl = $"/m/{normalizedSlug}/order-status/{token}/summary",
            ThemeColor = themeColor,
            LogoUrl = business.LogoUrl,
            Description = business.Description
        };

        ViewData["Title"] = isConfirmationPage
            ? $"Siparişiniz Alındı | {business.Name}"
            : $"Sipariş Takibi | {business.Name}";

        return View("OrderStatus", model);
    }

    private static string BuildTrackingUrl(string slug, string token)
        => $"/m/{slug}/order-status/{token}";

    private static string BuildConfirmationUrl(string slug, string token)
        => $"/m/{slug}/order-confirmation/{token}";

    private static string GenerateOrderNumber()
    {
        var random = Random.Shared.Next(1000, 9999);
        return $"DP-{DateTime.UtcNow:yyyyMMddHHmmss}-{random}";
    }

    private static string? ResolveWhatsAppNumber(string? settingWhatsAppNumber, string? businessPhone)
    {
        var fromSetting = NormalizeWhatsAppNumber(settingWhatsAppNumber);
        if (fromSetting is not null)
        {
            return fromSetting;
        }

        return NormalizeWhatsAppNumber(businessPhone);
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

    private static string ResolveCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return "TRY";
        }

        return currency.Trim().ToUpperInvariant();
    }

    private static string ResolveThemeColor(string? themeColor)
    {
        const string defaultColor = "#2563eb";

        if (string.IsNullOrWhiteSpace(themeColor))
        {
            return defaultColor;
        }

        return Regex.IsMatch(themeColor.Trim(), @"^#[0-9A-Fa-f]{6}$")
            ? themeColor.Trim()
            : defaultColor;
    }

    private static string FormatAmount(decimal amount, string currency, CultureInfo culture)
    {
        var formatted = amount.ToString("N2", culture);
        return currency.Equals("TRY", StringComparison.OrdinalIgnoreCase)
            ? $"{formatted} ₺"
            : $"{formatted} {currency}";
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
        string currency,
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
            sb.AppendLine($"{item.Quantity}x {item.Name} — {FormatAmount(lineTotal, currency, culture)}");
        }

        sb.AppendLine("---");
        sb.AppendLine($"Toplam: {FormatAmount(total, currency, culture)}");

        if (!string.IsNullOrWhiteSpace(notes))
        {
            sb.AppendLine();
            sb.AppendLine($"Not: {notes}");
        }

        return sb.ToString().TrimEnd();
    }
}
