using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Areas.Business.Models;

public class LoyaltyTransactionListViewModel
{
    public DateTime CreatedAt { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public LoyaltyTransactionType Type { get; set; }
    public int Points { get; set; }
    public string? Description { get; set; }
}
