namespace DukkanPilot.Web.Areas.Business.Models;

public class BusinessSubscriptionStatusViewModel
{
    public bool HasValidSubscription { get; set; }

    public string PlanName { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;

    public string StatusCssClass { get; set; } = "bg-secondary";

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? DaysRemaining { get; set; }

    public bool IsTrial { get; set; }

    public string Message { get; set; } = string.Empty;
}
