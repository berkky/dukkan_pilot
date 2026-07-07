namespace DukkanPilot.Web.Areas.Business.Models;

public class BillingIndexViewModel
{
    public BusinessSubscriptionStatusViewModel Subscription { get; set; } = new();

    public BusinessPlanUsageViewModel Usage { get; set; } = new();

    public List<AvailablePlanViewModel> AvailablePlans { get; set; } = new();
}
