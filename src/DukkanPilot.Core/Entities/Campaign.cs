using DukkanPilot.Core.Common;
using DukkanPilot.Core.Enums;

namespace DukkanPilot.Core.Entities;

public class Campaign : BaseEntity
{
    public int BusinessId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public string? ImageUrl { get; set; }
    public CampaignDiscountType DiscountType { get; set; } = CampaignDiscountType.Percentage;
    public decimal DiscountValue { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public decimal? MaximumDiscountAmount { get; set; }
    public bool IsPublicVisible { get; set; } = true;
    public bool IsAutoApply { get; set; }
    public int Priority { get; set; }

    public Business Business { get; set; } = null!;
}
