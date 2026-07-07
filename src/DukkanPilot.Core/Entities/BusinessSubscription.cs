using DukkanPilot.Core.Common;
using DukkanPilot.Core.Enums;

namespace DukkanPilot.Core.Entities;

public class BusinessSubscription : BaseEntity
{
    public int BusinessId { get; set; }
    public int SubscriptionPlanId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trial;

    public Business Business { get; set; } = null!;
    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;
}
