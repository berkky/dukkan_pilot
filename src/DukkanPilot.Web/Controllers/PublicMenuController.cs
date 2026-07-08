using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Models.PublicMenu;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Controllers;

public class PublicMenuController : Controller
{
    private const string RewardRequestPrefix = "Ödül talebi:";
    private const string CampaignInfoPrefix = "Kampanya:";
    private const string SubtotalPrefix = "Ara toplam:";
    private const string DiscountPrefix = "İndirim:";
    private const string TotalPrefix = "Toplam:";

    private readonly AppDbContext _context;
    private readonly PublicOrderTrackingTokenHelper _trackingTokenHelper;
    private readonly PublicOrderPricingHelper _pricingHelper;
    private readonly IAuditLogService _auditLog;

    public PublicMenuController(
        AppDbContext context,
        PublicOrderTrackingTokenHelper trackingTokenHelper,
        PublicOrderPricingHelper pricingHelper,
        IAuditLogService auditLog)
    {
        _context = context;
        _trackingTokenHelper = trackingTokenHelper;
        _pricingHelper = pricingHelper;
        _auditLog = auditLog;
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
                && c.IsPublicVisible
                && c.StartDate <= now
                && (c.EndDate == null || c.EndDate >= now))
            .OrderByDescending(c => c.Priority)
            .ThenByDescending(c => c.StartDate)
            .ToListAsync();

        var campaignViewModels = campaigns.Select(c => new PublicMenuCampaignViewModel
        {
            Id = c.Id,
            Title = c.Title,
            Description = c.Description,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            ImageUrl = c.ImageUrl,
            DiscountTypeText = CampaignDiscountHelper.GetDiscountTypeLabel(c.DiscountType),
            DiscountValueText = CampaignDiscountHelper.GetDiscountValueText(c.DiscountType, c.DiscountValue),
            DiscountValue = c.DiscountValue,
            MinimumOrderAmount = c.MinimumOrderAmount,
            MaximumDiscountAmount = c.MaximumDiscountAmount,
            IsAutoApply = c.IsAutoApply,
            IsPublicVisible = c.IsPublicVisible,
            CampaignBadgeText = CampaignDiscountHelper.GetCampaignBadgeText(c.DiscountType, c.DiscountValue),
            CampaignDescriptionText = c.Description,
            MinimumOrderText = CampaignDiscountHelper.GetMinimumOrderText(c.MinimumOrderAmount)
        }).ToList();

        var rewards = await _context.Rewards
            .AsNoTracking()
            .Where(r => r.BusinessId == business.Id && r.IsActive)
            .OrderBy(r => r.RequiredPoints)
            .ThenBy(r => r.Name)
            .Select(r => new PublicRewardViewModel
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                RequiredPoints = r.RequiredPoints
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
            Campaigns = campaignViewModels,
            Rewards = rewards,
            Categories = categories
        };

        ViewData["Title"] = $"{business.Name} Menü | DukkanPilot";

        return View(model);
    }

    [HttpPost("/m/{slug}/preview-order")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PreviewOrder(string slug, [FromBody] PlaceOrderRequest request)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Slug == normalizedSlug && b.IsActive);

        if (business is null)
        {
            return NotFound(new { errors = new[] { "İşletme bulunamadı." } });
        }

        var pricing = await _pricingHelper.CalculateAsync(
            business.Id,
            request.Items,
            request.RewardRequestName);

        if (!pricing.IsValid)
        {
            return BadRequest(new PublicOrderPreviewResponse
            {
                Errors = pricing.Errors
            });
        }

        return Ok(new PublicOrderPreviewResponse
        {
            Subtotal = pricing.Subtotal,
            DiscountAmount = pricing.DiscountAmount,
            Total = pricing.Total,
            CampaignMessage = pricing.CampaignMessage,
            AppliedCampaignName = pricing.AppliedCampaignName,
            DiscountTypeText = pricing.DiscountTypeText,
            EarnedPointsPreview = pricing.EarnedPointsPreview,
            LoyaltyPreviewMessage = pricing.LoyaltyPreviewMessage
        });
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

        var pricing = await _pricingHelper.CalculateAsync(
            business.Id,
            request.Items,
            request.RewardRequestName);

        if (!pricing.IsValid)
        {
            return BadRequest(new { error = pricing.Errors.FirstOrDefault() ?? "Sepet doğrulanamadı." });
        }

        var currency = ResolveCurrency(business.Setting?.Currency);
        var orderNumber = GenerateOrderNumber();

        var order = new Order
        {
            BusinessId = business.Id,
            CustomerId = null,
            OrderNumber = orderNumber,
            TotalAmount = pricing.Total,
            SubtotalAmount = pricing.Subtotal,
            DiscountAmount = pricing.DiscountAmount,
            AppliedCampaignId = pricing.AppliedCampaignId,
            AppliedCampaignName = TrimToMax(pricing.AppliedCampaignName, 200),
            Status = OrderStatus.Pending,
            Source = OrderSource.WhatsApp,
            Notes = BuildOrderNotes(request.Notes, pricing),
            CustomerName = TrimToMax(request.CustomerName, 200),
            CustomerPhone = TrimToMax(request.CustomerPhone, 20)
        };

        foreach (var item in pricing.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        await _auditLog.LogPublicAsync(
            business.Id,
            "Public.OrderCreated",
            "Order",
            order.Id,
            $"Yeni sipariş oluşturuldu: {orderNumber}",
            new
            {
                orderId = order.Id,
                orderNumber,
                totalAmount = order.TotalAmount,
                itemCount = order.Items.Count
            });

        var messageLines = pricing.Items
            .Select(i => (i.ProductName, i.Quantity, i.UnitPrice))
            .ToList();

        var message = BuildWhatsAppMessage(
            business.Name,
            orderNumber,
            messageLines,
            pricing.Subtotal,
            pricing.DiscountAmount,
            pricing.Total,
            currency,
            order.CustomerName,
            order.CustomerPhone,
            pricing.RewardRequestName,
            pricing.CampaignMessage,
            ExtractCustomerNote(order.Notes));

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

        var rewardRequestText = ExtractRewardRequest(order.Notes);
        var customerNote = ExtractCustomerNote(order.Notes);
        var campaignInfoText = !string.IsNullOrWhiteSpace(order.AppliedCampaignName)
            ? order.AppliedCampaignName
            : ExtractCampaignInfo(order.Notes);

        var loyaltyRule = await _pricingHelper.GetActiveLoyaltyRuleAsync(business.Id);
        int? earnedPointsPreview = null;
        string? loyaltyPreviewMessage = null;

        if (loyaltyRule is not null && order.Status != OrderStatus.Cancelled)
        {
            if (order.TotalAmount >= loyaltyRule.MinimumOrderAmount)
            {
                var points = Areas.Business.Models.OrderLoyaltyHelper.CalculateEarnedPoints(
                    order.TotalAmount,
                    loyaltyRule.PointsPerAmount);

                if (points > 0)
                {
                    earnedPointsPreview = points;
                    loyaltyPreviewMessage = order.Status == OrderStatus.Completed
                        ? $"Bu sipariş tamamlandığında yaklaşık {points} sadakat puanı kazanılmış olabilir."
                        : $"Sipariş tamamlandığında yaklaşık {points} sadakat puanı kazanabilirsiniz.";
                }
            }
            else
            {
                loyaltyPreviewMessage =
                    $"Sadakat puanı için minimum sipariş tutarı {loyaltyRule.MinimumOrderAmount:N0} ₺.";
            }
        }

        var subtotal = order.SubtotalAmount > 0
            ? order.SubtotalAmount
            : messageLines.Sum(i => i.Quantity * i.UnitPrice);
        var discountAmount = order.DiscountAmount > 0
            ? order.DiscountAmount
            : (subtotal > order.TotalAmount ? subtotal - order.TotalAmount : 0m);

        string? whatsAppUrl = null;
        if (whatsAppNumber is not null)
        {
            var message = BuildWhatsAppMessage(
                business.Name,
                order.OrderNumber,
                messageLines,
                subtotal,
                discountAmount,
                order.TotalAmount,
                currency,
                order.CustomerName,
                order.CustomerPhone,
                rewardRequestText,
                campaignInfoText,
                customerNote);
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
            SubtotalAmount = subtotal,
            DiscountAmount = discountAmount,
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
            TrackingPageUrl = trackingUrl,
            PublicMenuUrl = $"/m/{normalizedSlug}",
            SummaryUrl = $"/m/{normalizedSlug}/order-status/{token}/summary",
            ThemeColor = themeColor,
            LogoUrl = business.LogoUrl,
            Description = business.Description,
            HasLoyaltyProgram = loyaltyRule is not null,
            EarnedPointsPreview = earnedPointsPreview,
            LoyaltyPreviewMessage = loyaltyPreviewMessage,
            RewardRequestText = rewardRequestText,
            CampaignInfoText = campaignInfoText
        };

        ViewData["Title"] = isConfirmationPage
            ? $"Siparişiniz Alındı | {business.Name}"
            : $"Sipariş Takibi | {business.Name}";

        return View("OrderStatus", model);
    }

    private static string? BuildOrderNotes(string? customerNotes, PublicOrderPricingResult pricing)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(customerNotes))
        {
            parts.Add(customerNotes.Trim());
        }

        if (!string.IsNullOrWhiteSpace(pricing.RewardRequestName))
        {
            parts.Add($"{RewardRequestPrefix} {pricing.RewardRequestName.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(pricing.AppliedCampaignName))
        {
            parts.Add($"{CampaignInfoPrefix} {pricing.AppliedCampaignName.Trim()}");
        }

        if (pricing.Subtotal > 0)
        {
            parts.Add($"{SubtotalPrefix} {pricing.Subtotal:N2}");
        }

        if (pricing.DiscountAmount > 0)
        {
            parts.Add($"{DiscountPrefix} {pricing.DiscountAmount:N2}");
        }

        if (pricing.Total >= 0)
        {
            parts.Add($"{TotalPrefix} {pricing.Total:N2}");
        }

        if (parts.Count == 0)
        {
            return null;
        }

        return TrimToMax(string.Join(" | ", parts), 1000);
    }

    private static string? ExtractRewardRequest(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        foreach (var part in notes.Split(" | ", StringSplitOptions.TrimEntries))
        {
            if (part.StartsWith(RewardRequestPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return part[RewardRequestPrefix.Length..].Trim();
            }
        }

        return null;
    }

    private static string? ExtractCampaignInfo(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        foreach (var part in notes.Split(" | ", StringSplitOptions.TrimEntries))
        {
            if (part.StartsWith(CampaignInfoPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return part[CampaignInfoPrefix.Length..].Trim();
            }
        }

        return null;
    }

    private static string? ExtractCustomerNote(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var customerParts = notes.Split(" | ", StringSplitOptions.TrimEntries)
            .Where(part =>
                !part.StartsWith(RewardRequestPrefix, StringComparison.OrdinalIgnoreCase) &&
                !part.StartsWith(CampaignInfoPrefix, StringComparison.OrdinalIgnoreCase) &&
                !part.StartsWith(SubtotalPrefix, StringComparison.OrdinalIgnoreCase) &&
                !part.StartsWith(DiscountPrefix, StringComparison.OrdinalIgnoreCase) &&
                !part.StartsWith(TotalPrefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return customerParts.Count > 0 ? string.Join(" | ", customerParts) : null;
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
        decimal subtotal,
        decimal discountAmount,
        decimal total,
        string currency,
        string? customerName,
        string? customerPhone,
        string? rewardRequest,
        string? campaignInfo,
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
        sb.AppendLine($"Ara Toplam: {FormatAmount(subtotal, currency, culture)}");

        if (discountAmount > 0)
        {
            sb.AppendLine($"İndirim: -{FormatAmount(discountAmount, currency, culture)}");
        }

        sb.AppendLine($"Toplam: {FormatAmount(total, currency, culture)}");

        if (!string.IsNullOrWhiteSpace(campaignInfo))
        {
            sb.AppendLine();
            sb.AppendLine($"Kampanya: {campaignInfo}");
        }

        if (!string.IsNullOrWhiteSpace(rewardRequest))
        {
            sb.AppendLine();
            sb.AppendLine($"Ödül talebi: {rewardRequest}");
        }

        if (!string.IsNullOrWhiteSpace(notes))
        {
            sb.AppendLine();
            sb.AppendLine($"Not: {notes}");
        }

        return sb.ToString().TrimEnd();
    }
}
