namespace DukkanPilot.Web.Areas.Business.Models;

public class BusinessDashboardViewModel
{
    public string BusinessName { get; set; } = string.Empty;
    public int TotalCategoryCount { get; set; }
    public int ActiveCategoryCount { get; set; }
    public int TotalProductCount { get; set; }
    public int ActiveProductCount { get; set; }
    public int TotalOrderCount { get; set; }
    public int TotalCustomerCount { get; set; }
    public int ActiveCustomerCount { get; set; }
    public List<RecentOrderViewModel> RecentOrders { get; set; } = new();
    public LoyaltySummaryViewModel LoyaltySummary { get; set; } = new();
    public CampaignDashboardSummaryViewModel CampaignSummary { get; set; } = new();
    public BusinessSubscriptionStatusViewModel Subscription { get; set; } = new();
    public bool IsBusinessOwner { get; set; }
}
