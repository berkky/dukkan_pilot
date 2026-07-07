namespace DukkanPilot.Web.Areas.Business.Models;

public class ReportsOverviewViewModel
{
    public int TodayOrderCount { get; set; }
    public decimal TodayRevenue { get; set; }
    public int Last7DaysOrderCount { get; set; }
    public decimal Last7DaysRevenue { get; set; }
    public int TotalCustomerCount { get; set; }
    public int TotalProductCount { get; set; }
    public int ActiveCampaignCount { get; set; }
    public int TotalEarnedPoints { get; set; }
}
