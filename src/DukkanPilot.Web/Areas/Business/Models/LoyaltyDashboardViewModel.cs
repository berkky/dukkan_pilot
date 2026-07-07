using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Areas.Business.Models;

public class LoyaltyDashboardViewModel
{
    public bool HasActiveRule { get; set; }
    public decimal? PointsPerAmount { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public string? RuleDescription { get; set; }
    public int ActiveCustomerCount { get; set; }
    public int TotalEarnedPoints { get; set; }
    public int TotalRedeemedPoints { get; set; }
    public int ActiveRewardCount { get; set; }
    public string? LowestActiveRewardName { get; set; }
    public int? LowestActiveRewardPoints { get; set; }
    public List<LoyaltyTransactionListViewModel> RecentTransactions { get; set; } = new();
}
