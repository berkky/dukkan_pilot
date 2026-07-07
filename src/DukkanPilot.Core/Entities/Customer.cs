using DukkanPilot.Core.Common;

namespace DukkanPilot.Core.Entities;

public class Customer : BaseEntity
{
    public int BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int TotalPoints { get; set; }
    public string? Notes { get; set; }

    public Business Business { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();
}
