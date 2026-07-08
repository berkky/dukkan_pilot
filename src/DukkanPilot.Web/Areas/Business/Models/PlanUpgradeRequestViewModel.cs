namespace DukkanPilot.Web.Areas.Business.Models;

public class PlanUpgradeRequestViewModel
{
    public int BusinessId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string BusinessSlug { get; set; } = string.Empty;

    public string OwnerEmail { get; set; } = string.Empty;

    public string CurrentPlanName { get; set; } = string.Empty;

    public int RequestedPlanId { get; set; }

    public string RequestedPlanName { get; set; } = string.Empty;

    public decimal RequestedPlanPrice { get; set; }

    public DateTime RequestedAtUtc { get; set; }

    public string RequestMessage { get; set; } = string.Empty;

    public int? SalesRequestId { get; set; }
}
