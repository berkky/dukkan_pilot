namespace DukkanPilot.Web.Areas.Business.Models;

public class BusinessDashboardViewModel
{
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessSlug { get; set; } = string.Empty;
    public int TotalCategoryCount { get; set; }
    public int ActiveCategoryCount { get; set; }
    public int TotalProductCount { get; set; }
    public int ActiveProductCount { get; set; }
    public int TotalOrderCount { get; set; }
    public int TotalCustomerCount { get; set; }
    public int ActiveCustomerCount { get; set; }
    public List<RecentOrderViewModel> RecentOrders { get; set; } = new();
    public DashboardOrderSummaryViewModel OrderSummary { get; set; } = new();
    public DashboardStatusDistributionViewModel StatusDistribution { get; set; } = new();
    public List<ProductSalesRowViewModel> TopProducts { get; set; } = new();
    public LoyaltySummaryViewModel LoyaltySummary { get; set; } = new();
    public CampaignDashboardSummaryViewModel CampaignSummary { get; set; } = new();
    public BusinessSubscriptionStatusViewModel Subscription { get; set; } = new();
    public BusinessPlanUsageViewModel PlanUsage { get; set; } = new();
    public GoLiveDashboardCardViewModel? GoLiveStatus { get; set; }
    public DukkanPilot.Web.Models.Onboarding.CustomerOnboardingDashboardCard? OnboardingStatus { get; set; }
    public DukkanPilot.Web.Models.Success.CustomerSuccessDashboardCard? SuccessStatus { get; set; }
    public DashboardNotificationCardViewModel Notifications { get; set; } = new();
    public DashboardSupportCardViewModel Support { get; set; } = new();
    public bool IsBusinessOwner { get; set; }
}
