using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Areas.Business.Models;

public class CustomerLoyaltyTransactionViewModel
{
    public DateTime CreatedAt { get; set; }
    public LoyaltyTransactionType Type { get; set; }
    public int Points { get; set; }
    public string? Description { get; set; }
    public string? RewardName { get; set; }
}
