using DukkanPilot.Core.Common;
using DukkanPilot.Core.Enums;

namespace DukkanPilot.Core.Entities;

public class Order : BaseEntity
{
    public int BusinessId { get; set; }
    public int? CustomerId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public int? AppliedCampaignId { get; set; }
    public string? AppliedCampaignName { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public OrderSource Source { get; set; } = OrderSource.WhatsApp;
    public string? Notes { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }

    public Business Business { get; set; } = null!;
    public Customer? Customer { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
