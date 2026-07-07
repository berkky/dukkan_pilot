using DukkanPilot.Core.Common;
using DukkanPilot.Core.Enums;

namespace DukkanPilot.Core.Entities;

public class LoyaltyTransaction : BaseEntity
{
    public int BusinessId { get; set; }
    public int CustomerId { get; set; }
    public int? RewardId { get; set; }
    public int Points { get; set; }
    public LoyaltyTransactionType Type { get; set; }
    public string? Description { get; set; }

    public Business Business { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Reward? Reward { get; set; }
}
