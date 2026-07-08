namespace DukkanPilot.Web.Models.PublicMenu;

public class PublicMenuCampaignViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? ImageUrl { get; set; }

    public string DiscountTypeText { get; set; } = string.Empty;

    public string DiscountValueText { get; set; } = string.Empty;

    public decimal DiscountValue { get; set; }

    public decimal? MinimumOrderAmount { get; set; }

    public decimal? MaximumDiscountAmount { get; set; }

    public bool IsAutoApply { get; set; }

    public bool IsPublicVisible { get; set; } = true;

    public string CampaignBadgeText { get; set; } = string.Empty;

    public string? CampaignDescriptionText { get; set; }

    public string? MinimumOrderText { get; set; }

    public bool HasActiveCampaignPeriod => true;
}
