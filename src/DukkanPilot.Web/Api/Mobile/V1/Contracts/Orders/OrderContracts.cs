using System.ComponentModel.DataAnnotations;

namespace DukkanPilot.Web.Api.Mobile.V1.Contracts.Orders;

public sealed record MobileOrderListItem(
    int Id,
    string OrderNumber,
    decimal TotalAmount,
    string Status,
    string Source,
    string? ServiceType,
    string? TableLabel,
    string? CustomerName,
    DateTime CreatedAtUtc);

public sealed record MobileOrderItem(
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record MobileOrderDetails(
    int Id,
    string OrderNumber,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    string Status,
    string Source,
    string? ServiceType,
    string? TableLabel,
    string? CustomerName,
    string? CustomerPhone,
    string? Notes,
    DateTime CreatedAtUtc,
    IReadOnlyList<MobileOrderItem> Items);

public sealed class MobileOrderStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

public sealed record MobileKitchenResponse(
    int PendingCount,
    int PreparingCount,
    IReadOnlyList<MobileOrderDetails> Orders,
    DateTime ServerTimeUtc);

public sealed record MobileDashboardTodayResponse(
    int TotalOrders,
    int PendingOrders,
    int PreparingOrders,
    int CompletedOrders,
    int CancelledOrders,
    decimal Revenue,
    DateTime ServerTimeUtc);
