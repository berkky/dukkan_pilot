namespace DukkanPilot.Web.Areas.Admin.Models;

public class AdminOperationsViewModel
{
    public string EnvironmentName { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string AssemblyVersion { get; set; } = string.Empty;
    public DateTime UtcNow { get; set; }

    public bool DatabaseCanConnect { get; set; }
    public int AppliedMigrationCount { get; set; }
    public int PendingMigrationCount { get; set; }
    public string? LastAppliedMigration { get; set; }
    public List<string> PendingMigrationNames { get; set; } = new();
    public string? DatabaseStatusMessage { get; set; }

    public bool HasProductionExample { get; set; }
    public bool HasDeploymentChecklist { get; set; }
    public bool HasReleaseChecklist { get; set; }
    public bool HasSmokeChecklist { get; set; }
    public bool HasBackupDocs { get; set; }
    public bool HasMigrationRunbook { get; set; }
    public bool HasIncidentRunbook { get; set; }
    public bool HasReliabilityRunbook { get; set; }
    public bool HasPerformanceHardeningDocs { get; set; }
    public bool HasPerformanceSmokeDocs { get; set; }
    public bool HasPerformanceSmokeScript { get; set; }
    public bool HasOperationalSecurityChecklist { get; set; }
    public bool HasFirstReleaseOpsDocs { get; set; }
    public bool HasLegalReadinessDocs { get; set; }
    public bool HasPrivacyDataMapDocs { get; set; }
    public bool HasCookieDocs { get; set; }
    public bool HasTermsNotesDocs { get; set; }
    public bool HasLegalPrivacyView { get; set; }
    public bool HasTrustView { get; set; }
    public bool HasCookieNoticeAssets { get; set; }
    public bool HasSupportEmailPlaceholder { get; set; }

    public List<OpsChecklistItemViewModel> OperationalChecklist { get; set; } = new();
    public List<OpsChecklistItemViewModel> LegalReadinessChecklist { get; set; } = new();
    public List<OpsChecklistItemViewModel> PerformanceReliabilityChecklist { get; set; } = new();
    public List<OpsDocLinkViewModel> DocLinks { get; set; } = new();
    public List<string> ScriptHints { get; set; } = new();
}

public class OpsChecklistItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsReadyHint { get; set; }
}

public class OpsDocLinkViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}
