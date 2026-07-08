namespace DukkanPilot.Web.Areas.Business.Models;

public class GoLiveViewModel
{
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessSlug { get; set; } = string.Empty;
    public string PublicMenuUrl { get; set; } = string.Empty;
    public int HealthScore { get; set; }
    public string HealthLabel { get; set; } = string.Empty;
    public string HealthBadgeClass { get; set; } = "bg-secondary";
    public int CompletedStepCount { get; set; }
    public int TotalStepCount { get; set; }
    public int RequiredCompletedCount { get; set; }
    public int RequiredTotalCount { get; set; }
    public int ProgressPercent { get; set; }
    public bool IsReadyToGoLive { get; set; }
    public bool IsBusinessOwner { get; set; }
    public string? PrimaryMissingStepTitle { get; set; }
    public string? PrimaryMissingStepActionUrl { get; set; }
    public string? PrimaryMissingStepActionText { get; set; }
    public bool PrimaryMissingStepOwnerOnly { get; set; }
    public List<GoLiveStepViewModel> SetupSteps { get; set; } = new();
    public List<GoLiveQuickActionViewModel> QuickActions { get; set; } = new();
    public GoLivePreviewViewModel PublicMenuPreview { get; set; } = new();
    public List<GoLiveTestItemViewModel> TestChecklist { get; set; } = new();
    public BusinessPlanUsageViewModel PlanUsage { get; set; } = new();
    public List<string> LaunchTips { get; set; } = new();
}

public class GoLiveStepViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsRequired { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string BadgeClass { get; set; } = "bg-secondary";
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
    public string? SecondaryActionText { get; set; }
    public string? SecondaryActionUrl { get; set; }
    public bool OwnerOnly { get; set; }
    public string? WarningText { get; set; }
}

public class GoLiveQuickActionViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ButtonClass { get; set; } = "btn-outline-primary";
    public bool IsExternal { get; set; }
    public bool OwnerOnly { get; set; }
}

public class GoLivePreviewViewModel
{
    public string BusinessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string ThemeColor { get; set; } = "#2563eb";
    public string Currency { get; set; } = "TRY";
    public int CategoryCount { get; set; }
    public int ActiveProductCount { get; set; }
    public int CampaignCount { get; set; }
    public int RewardCount { get; set; }
}

public class GoLiveTestItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
}

public class GoLiveDashboardCardViewModel
{
    public int HealthScore { get; set; }
    public string HealthLabel { get; set; } = string.Empty;
    public string HealthBadgeClass { get; set; } = "bg-secondary";
    public int ProgressPercent { get; set; }
    public int RequiredCompletedCount { get; set; }
    public int RequiredTotalCount { get; set; }
    public bool IsReadyToGoLive { get; set; }
    public string? PrimaryMissingStepTitle { get; set; }
}
