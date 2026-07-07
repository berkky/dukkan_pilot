using DukkanPilot.Core.Common;

namespace DukkanPilot.Core.Entities;

public class Reward : BaseEntity
{
    public int BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RequiredPoints { get; set; }

    public Business Business { get; set; } = null!;
    public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();
}
