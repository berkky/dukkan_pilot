using DukkanPilot.Core.Common;

namespace DukkanPilot.Core.Entities;

public class LoyaltyRule : BaseEntity
{
    public int BusinessId { get; set; }
    public decimal PointsPerAmount { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public string? Description { get; set; }

    public Business Business { get; set; } = null!;
}
