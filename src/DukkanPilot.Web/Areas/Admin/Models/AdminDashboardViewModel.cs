namespace DukkanPilot.Web.Areas.Admin.Models;

public class AdminDashboardViewModel
{
    public int TotalBusinessCount { get; set; }
    public int ActiveBusinessCount { get; set; }
    public int TotalPlanCount { get; set; }
    public int ActiveSubscriptionCount { get; set; }
    public int TotalProductCount { get; set; }
    public int TotalOrderCount { get; set; }
}
