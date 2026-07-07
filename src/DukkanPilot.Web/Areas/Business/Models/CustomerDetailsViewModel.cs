namespace DukkanPilot.Web.Areas.Business.Models;

public class CustomerDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public int TotalPoints { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CustomerOrderHistoryViewModel> OrderHistory { get; set; } = new();
    public List<CustomerLoyaltyTransactionViewModel> LoyaltyTransactions { get; set; } = new();
}
