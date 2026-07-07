using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Areas.Business.Models;

public class KitchenOrderCardViewModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public string? Notes { get; set; }
    public List<OrderItemViewModel> Items { get; set; } = new();

    public string StatusText => OrderDisplayHelper.GetStatusLabel(Status);
    public string StatusBadgeClass => OrderDisplayHelper.GetStatusBadgeClass(Status);
    public string? WhatsAppContactUrl => OrderDisplayHelper.BuildWhatsAppContactUrl(CustomerPhone);
}

public class OrderKitchenViewModel
{
    public List<KitchenOrderCardViewModel> PendingOrders { get; set; } = new();
    public List<KitchenOrderCardViewModel> PreparingOrders { get; set; } = new();
    public List<KitchenOrderCardViewModel> CompletedTodayOrders { get; set; } = new();
    public List<KitchenOrderCardViewModel> CancelledTodayOrders { get; set; } = new();
    public int PendingCount { get; set; }
    public int PreparingCount { get; set; }
    public int CompletedTodayCount { get; set; }
    public decimal TodayRevenue { get; set; }
    public int? LatestOrderId { get; set; }
    public DateTime? LatestOrderCreatedAt { get; set; }
}
