namespace DukkanPilot.Web.Models.Onboarding;

public enum OnboardingStatus
{
    NotStarted,
    SetupInProgress,
    AlmostReady,
    ReadyToLaunch,
    Live
}

public class CustomerOnboardingStep
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsRequired { get; set; }
    public int ScoreWeight { get; set; }
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
    public string? HelpText { get; set; }
    public string BadgeText { get; set; } = string.Empty;
    public string BadgeClass { get; set; } = "bg-secondary";
    public string Severity { get; set; } = "info";
    public bool OwnerOnly { get; set; }
    public string? WarningText { get; set; }
}

public class CustomerOnboardingQuickLink
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool OwnerOnly { get; set; }
}

public class CustomerOnboardingSnapshot
{
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessSlug { get; set; } = string.Empty;
    public bool BusinessIsActive { get; set; }
    public string PublicMenuUrl { get; set; } = string.Empty;
    public int Score { get; set; }
    public OnboardingStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = "bg-secondary";
    public string CardVariantClass { get; set; } = "border-secondary";
    public int CompletedStepCount { get; set; }
    public int TotalStepCount { get; set; }
    public int RequiredCompletedCount { get; set; }
    public int RequiredTotalCount { get; set; }
    public int MissingRequiredCount { get; set; }
    public bool IsLive { get; set; }
    public bool IsReadyToLaunch { get; set; }
    public bool IsAtRisk { get; set; }
    public string? NextBestActionTitle { get; set; }
    public string? NextBestActionUrl { get; set; }
    public string? NextBestActionText { get; set; }
    public bool NextBestActionOwnerOnly { get; set; }
    public int ActiveCategoryCount { get; set; }
    public int ActiveProductCount { get; set; }
    public int OrderCount { get; set; }
    public int CampaignCount { get; set; }
    public int RewardCount { get; set; }
    public int StaffCount { get; set; }
    public int CustomerCount { get; set; }
    public DateTime? LastActivityAtUtc { get; set; }
    public string? PlanName { get; set; }
    public string? SubscriptionStatusLabel { get; set; }
    public List<CustomerOnboardingStep> Steps { get; set; } = new();
    public List<string> LaunchChecklist { get; set; } = new();
    public List<CustomerOnboardingQuickLink> QuickLinks { get; set; } = new();
}

public class CustomerOnboardingDashboardCard
{
    public int Score { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = "bg-secondary";
    public string CardVariantClass { get; set; } = "border-secondary";
    public string? NextBestActionTitle { get; set; }
    public bool IsLowScore { get; set; }
    public bool IsReadyOrLive { get; set; }
}
