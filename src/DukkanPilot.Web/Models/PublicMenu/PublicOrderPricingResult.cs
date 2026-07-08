namespace DukkanPilot.Web.Models.PublicMenu;

public class PublicOrderPricingResult
{
    public bool IsValid { get; set; }

    public List<string> Errors { get; set; } = [];

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal Total { get; set; }

    public int? AppliedCampaignId { get; set; }

    public string? AppliedCampaignName { get; set; }

    public string? AppliedCampaignDescription { get; set; }

    public string? DiscountTypeText { get; set; }

    public string? CampaignMessage { get; set; }

    public int? EarnedPointsPreview { get; set; }

    public string? LoyaltyPreviewMessage { get; set; }

    public string? RewardRequestName { get; set; }

    public List<PublicOrderPricingItem> Items { get; set; } = [];
}

public class PublicOrderPricingItem
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }
}

public class PublicOrderPreviewResponse
{
    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal Total { get; set; }

    public string? CampaignMessage { get; set; }

    public string? AppliedCampaignName { get; set; }

    public string? DiscountTypeText { get; set; }

    public int? EarnedPointsPreview { get; set; }

    public string? LoyaltyPreviewMessage { get; set; }

    public List<string> Errors { get; set; } = [];
}
