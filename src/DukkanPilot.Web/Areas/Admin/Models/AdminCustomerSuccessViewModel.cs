using DukkanPilot.Web.Models.Success;

namespace DukkanPilot.Web.Areas.Admin.Models;

public class AdminCustomerSuccessViewModel
{
    public int TotalActiveBusinesses { get; set; }
    public int HealthyOrGrowthReadyCount { get; set; }
    public int AtRiskOrCriticalCount { get; set; }
    public int NoOrdersLast30DaysCount { get; set; }
    public int ExpiringIn7DaysCount { get; set; }
    public int UpgradeOpportunityCount { get; set; }
    public int WonLeadLowHealthCount { get; set; }

    public string? StatusFilter { get; set; }
    public string? ChurnRiskFilter { get; set; }
    public string? ExpansionFilter { get; set; }
    public string? SubscriptionFilter { get; set; }
    public bool? NoOrdersLast30Days { get; set; }
    public bool? HasUpgradeRequest { get; set; }
    public string? Search { get; set; }

    public List<AdminCustomerSuccessRowViewModel> Rows { get; set; } = new();
    public List<AdminCustomerSuccessAttentionItem> AttentionList { get; set; } = new();
}

public class AdminCustomerSuccessRowViewModel
{
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string PublicMenuUrl { get; set; } = string.Empty;
    public string? PlanName { get; set; }
    public string SubscriptionStatusText { get; set; } = string.Empty;
    public string SubscriptionStatusBadgeClass { get; set; } = "bg-secondary";
    public int Score { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = "bg-secondary";
    public string ChurnRiskLabel { get; set; } = string.Empty;
    public string ChurnRiskBadgeClass { get; set; } = "bg-secondary";
    public string ExpansionLabel { get; set; } = string.Empty;
    public string ExpansionBadgeClass { get; set; } = "bg-secondary";
    public DateTime? LastOrderAtUtc { get; set; }
    public DateTime? LastActivityAtUtc { get; set; }
    public int OrdersLast30Days { get; set; }
    public decimal RevenueLast30Days { get; set; }
    public string? TopRisk { get; set; }
    public string? NextRecommendedAction { get; set; }
    public bool HasUpgradeRequest { get; set; }
    public bool WonLeadLowHealth { get; set; }
}

public class AdminCustomerSuccessAttentionItem
{
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string SeverityBadgeClass { get; set; } = "bg-warning text-dark";
}

public class AdminCustomerSuccessDetailsViewModel
{
    public CustomerSuccessSnapshot Snapshot { get; set; } = new();
    public List<AdminOnboardingRelatedSalesRequestViewModel> RelatedSalesRequests { get; set; } = new();
    public List<AdminOnboardingActivityItemViewModel> RecentAudits { get; set; } = new();
    public List<AdminOnboardingActivityItemViewModel> RecentNotifications { get; set; } = new();
    public List<AdminSupportTicketRowViewModel> OpenSupportTickets { get; set; } = new();
}
