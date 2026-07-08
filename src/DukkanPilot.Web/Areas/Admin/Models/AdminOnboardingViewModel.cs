using DukkanPilot.Web.Models.Onboarding;

namespace DukkanPilot.Web.Areas.Admin.Models;

public class AdminOnboardingViewModel
{
    public int TotalBusinesses { get; set; }
    public int SetupInProgressCount { get; set; }
    public int AlmostReadyCount { get; set; }
    public int ReadyToLaunchCount { get; set; }
    public int LiveCount { get; set; }
    public int AtRiskCount { get; set; }
    public int WonSalesLast7Days { get; set; }

    public string? StatusFilter { get; set; }
    public string? Search { get; set; }
    public int? MinScore { get; set; }
    public int? MaxScore { get; set; }
    public bool? HasOpenSalesRequest { get; set; }
    public bool? HasNoActiveProduct { get; set; }
    public bool? HasNoOrder { get; set; }

    public List<AdminOnboardingRowViewModel> Rows { get; set; } = new();
    public List<AdminOnboardingHandoffRowViewModel> SalesHandoffs { get; set; } = new();
}

public class AdminOnboardingRowViewModel
{
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string PublicMenuUrl { get; set; } = string.Empty;
    public string? PlanName { get; set; }
    public string? SubscriptionStatusLabel { get; set; }
    public int Score { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = "bg-secondary";
    public OnboardingStatus Status { get; set; }
    public int MissingRequiredCount { get; set; }
    public string? NextBestActionTitle { get; set; }
    public int ActiveProductCount { get; set; }
    public int OrderCount { get; set; }
    public int CampaignCount { get; set; }
    public DateTime? LastActivityAtUtc { get; set; }
    public bool IsAtRisk { get; set; }
    public bool HasOpenSalesRequest { get; set; }
}

public class AdminOnboardingHandoffRowViewModel
{
    public int SalesRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string? BusinessNameFromRequest { get; set; }
    public int? BusinessId { get; set; }
    public string? LinkedBusinessName { get; set; }
    public int? OnboardingScore { get; set; }
    public string? OnboardingStatusLabel { get; set; }
    public string? OnboardingBadgeClass { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class AdminOnboardingDetailViewModel
{
    public CustomerOnboardingSnapshot Snapshot { get; set; } = new();
    public List<AdminOnboardingRelatedSalesRequestViewModel> RelatedSalesRequests { get; set; } = new();
    public List<AdminOnboardingActivityItemViewModel> RecentAudits { get; set; } = new();
    public List<AdminOnboardingActivityItemViewModel> RecentNotifications { get; set; } = new();
}

public class AdminOnboardingRelatedSalesRequestViewModel
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class AdminOnboardingActivityItemViewModel
{
    public DateTime AtUtc { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
}
