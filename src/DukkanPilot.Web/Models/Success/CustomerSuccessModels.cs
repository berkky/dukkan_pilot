using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Models.Onboarding;

namespace DukkanPilot.Web.Models.Success;

public enum CustomerSuccessHealthStatus
{
    Critical,
    AtRisk,
    Stable,
    Healthy,
    GrowthReady
}

public enum CustomerSuccessChurnRisk
{
    Low,
    Medium,
    High,
    Critical
}

public enum CustomerSuccessExpansionPotential
{
    None,
    Watch,
    GoodFit,
    StrongFit
}

public sealed class CustomerSuccessSignal
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsPositive { get; set; }
    public string Severity { get; set; } = "info";
    public string Description { get; set; } = string.Empty;
}

public sealed class CustomerSuccessRecommendation
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "info";
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
    public string Category { get; set; } = "general";
    public bool IsCritical { get; set; }
}

public sealed class CustomerSuccessKpiSnapshot
{
    public int OrdersLast7Days { get; set; }
    public int OrdersLast30Days { get; set; }
    public int CompletedOrdersLast30Days { get; set; }
    public int CancelledOrdersLast30Days { get; set; }
    public decimal RevenueLast30Days { get; set; }
    public decimal AverageBasketLast30Days { get; set; }
    public int NewCustomersLast30Days { get; set; }
    public int RepeatCustomers { get; set; }
    public int ActiveCustomerCount { get; set; }
    public int ActiveCategoryCount { get; set; }
    public int ActiveProductCount { get; set; }
    public int CampaignCount { get; set; }
    public int RewardCount { get; set; }
    public int StaffCount { get; set; }
    public int CriticalNotificationCount { get; set; }
}

public sealed class CustomerSuccessBreakdownItem
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class CustomerSuccessSnapshot
{
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessSlug { get; set; } = string.Empty;
    public string PublicMenuUrl { get; set; } = string.Empty;
    public int Score { get; set; }
    public CustomerSuccessHealthStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = "bg-secondary";
    public string CardVariantClass { get; set; } = "border-secondary";
    public CustomerSuccessChurnRisk ChurnRisk { get; set; }
    public string ChurnRiskLabel { get; set; } = string.Empty;
    public string ChurnRiskBadgeClass { get; set; } = "bg-secondary";
    public CustomerSuccessExpansionPotential ExpansionPotential { get; set; }
    public string ExpansionPotentialLabel { get; set; } = string.Empty;
    public string ExpansionPotentialBadgeClass { get; set; } = "bg-secondary";
    public bool IsAtRisk { get; set; }
    public bool IsHealthyOrBetter { get; set; }
    public string? TopRiskLabel { get; set; }
    public string? NextRecommendedActionTitle { get; set; }
    public string? NextRecommendedActionUrl { get; set; }
    public string? NextRecommendedActionText { get; set; }
    public DateTime? LastOrderAtUtc { get; set; }
    public DateTime? LastActivityAtUtc { get; set; }
    public CustomerSuccessKpiSnapshot Kpis { get; set; } = new();
    public BusinessSubscriptionStatusViewModel Subscription { get; set; } = new();
    public BusinessPlanUsageViewModel PlanUsage { get; set; } = new();
    public CustomerOnboardingSnapshot? Onboarding { get; set; }
    public List<CustomerSuccessSignal> RiskSignals { get; set; } = new();
    public List<CustomerSuccessSignal> GrowthSignals { get; set; } = new();
    public List<CustomerSuccessSignal> PositiveSignals { get; set; } = new();
    public List<CustomerSuccessRecommendation> Recommendations { get; set; } = new();
    public List<CustomerSuccessBreakdownItem> Breakdown { get; set; } = new();
    public List<(string Title, string Url, bool OwnerOnly)> QuickLinks { get; set; } = new();
}

public sealed class CustomerSuccessDashboardCard
{
    public int Score { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = "bg-secondary";
    public string CardVariantClass { get; set; } = "border-secondary";
    public string ChurnRiskLabel { get; set; } = string.Empty;
    public string ChurnRiskBadgeClass { get; set; } = "bg-secondary";
    public string? TopRiskLabel { get; set; }
    public string? NextActionTitle { get; set; }
    public bool IsCriticalOrAtRisk { get; set; }
    public bool IsHealthyOrBetter { get; set; }
}
