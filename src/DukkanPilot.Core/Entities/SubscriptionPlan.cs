using DukkanPilot.Core.Common;

namespace DukkanPilot.Core.Entities;

public class SubscriptionPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxProducts { get; set; }
    public int MaxCampaigns { get; set; }
    public decimal Price { get; set; }
    public int SortOrder { get; set; }

    public ICollection<BusinessSubscription> BusinessSubscriptions { get; set; } = new List<BusinessSubscription>();
}
