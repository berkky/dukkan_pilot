using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Areas.Business.Models;

public class CampaignDetailsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public CampaignDiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public decimal? MaximumDiscountAmount { get; set; }
    public bool IsPublicVisible { get; set; }
    public bool IsAutoApply { get; set; }
    public int Priority { get; set; }
}
