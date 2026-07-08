namespace DukkanPilot.Web.Areas.Admin.Models;

public class AdminQualityViewModel
{
    public string EnvironmentName { get; set; } = "n/a";
    public string MachineName { get; set; } = "n/a";
    public string AssemblyVersion { get; set; } = "n/a";
    public DateTime UtcNow { get; set; }

    public bool DatabaseCanConnect { get; set; }
    public int AppliedMigrationCount { get; set; }
    public int PendingMigrationCount { get; set; }
    public string? LastAppliedMigration { get; set; }
    public List<string> PendingMigrationNames { get; set; } = new();
    public string DatabaseStatusMessage { get; set; } = "n/a";

    public List<QualityReadinessCardViewModel> ReadinessCards { get; set; } = new();
    public List<QualityChecklistItemViewModel> QaChecklist { get; set; } = new();
    public List<QualityQuickLinkViewModel> QuickLinks { get; set; } = new();
    public List<string> ScriptHints { get; set; } = new();
}

public class QualityReadinessCardViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public string BadgeClass => IsReady ? "bg-success" : "bg-warning text-dark";
}

public class QualityChecklistItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class QualityQuickLinkViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ButtonClass { get; set; } = "btn-outline-secondary";
}

