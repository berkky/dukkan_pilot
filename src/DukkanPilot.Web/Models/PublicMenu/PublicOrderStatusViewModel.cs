using DukkanPilot.Core.Enums;
using DukkanPilot.Web.Helpers;

namespace DukkanPilot.Web.Models.PublicMenu;

public class PublicOrderStatusViewModel
{
    public bool IsConfirmationPage { get; set; }

    public bool IsExpired { get; set; }

    public string? ErrorMessage { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string BusinessSlug { get; set; } = string.Empty;

    public string OrderNumber { get; set; } = string.Empty;

    public string? CustomerName { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = "TRY";

    public OrderStatus Status { get; set; }

    public string StatusText => PublicOrderDisplayHelper.GetStatusLabel(Status);

    public string StatusBadgeClass => PublicOrderDisplayHelper.GetStatusBadgeClass(Status);

    public string StatusMessage => PublicOrderDisplayHelper.GetStatusMessage(Status);

    public IReadOnlyList<PublicOrderTimelineStepViewModel> TimelineSteps =>
        PublicOrderDisplayHelper.GetTimelineSteps(Status);

    public List<PublicOrderStatusItemViewModel> Items { get; set; } = [];

    public string? WhatsAppUrl { get; set; }

    public string TrackingUrl { get; set; } = string.Empty;

    public string PublicMenuUrl { get; set; } = string.Empty;

    public string SummaryUrl { get; set; } = string.Empty;

    public string ThemeColor { get; set; } = "#2563eb";

    public string? LogoUrl { get; set; }

    public string? Description { get; set; }

    public bool HasWhatsAppUrl => !string.IsNullOrWhiteSpace(WhatsAppUrl);
}

public class PublicOrderStatusItemViewModel
{
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal => Quantity * UnitPrice;
}

public class PublicOrderTimelineStepViewModel
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsCancelled { get; set; }
}

public class PublicOrderStatusSummaryResponse
{
    public string Status { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;

    public string StatusBadgeClass { get; set; } = string.Empty;

    public string StatusMessage { get; set; } = string.Empty;

    public List<PublicOrderTimelineStepViewModel> TimelineSteps { get; set; } = [];

    public decimal TotalAmount { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime ServerTime { get; set; }

    public bool IsCompleted { get; set; }

    public bool IsCancelled { get; set; }
}

public class PublicOrderTrackingNotFoundViewModel
{
    public string BusinessSlug { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = "Sipariş takip bağlantısı geçersiz.";

    public bool IsExpired { get; set; }
}
