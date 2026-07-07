namespace DukkanPilot.Web.Areas.Business.Models;

public class CustomerDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public int TotalPoints { get; set; }
    public int EarnedPoints { get; set; }
    public int RedeemedPoints { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Segment { get; set; } = string.Empty;
    public string SegmentLabel { get; set; } = string.Empty;
    public string SegmentBadgeClass { get; set; } = string.Empty;
    public string? WhatsAppContactUrl { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageBasket { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public DateTime? FirstOrderDate { get; set; }
    public decimal MaxOrderAmount { get; set; }
    public int Last30DaysOrderCount { get; set; }
    public string? TopProductName { get; set; }
    public List<CustomerOrderHistoryViewModel> OrderHistory { get; set; } = [];
    public List<CustomerLoyaltyTransactionViewModel> LoyaltyTransactions { get; set; } = [];
    public List<CustomerTopProductViewModel> TopProducts { get; set; } = [];
}
