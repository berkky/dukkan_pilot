namespace DukkanPilot.Web.Areas.Business.Models;

public class AvailablePlanViewModel
{
    public int PlanId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsCurrentPlan { get; set; }

    public bool IsActive { get; set; }

    public string ProductLimitText { get; set; } = string.Empty;

    public string CategoryLimitText { get; set; } = string.Empty;

    public string StaffLimitText { get; set; } = string.Empty;

    public string CampaignLimitText { get; set; } = string.Empty;

    public string RewardLimitText { get; set; } = string.Empty;

    public string QrCodeLimitText { get; set; } = string.Empty;
}
