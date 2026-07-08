namespace DukkanPilot.Web.Areas.Admin.Models;

public class SalesCenterViewModel
{
    public int TotalBusinesses { get; set; }
    public int ActiveBusinesses { get; set; }
    public int TrialBusinesses { get; set; }
    public int ExpiringSoonBusinesses { get; set; }
    public int DemoReadyBusinesses { get; set; }
    public List<SalesCenterBusinessRowViewModel> DemoReadyList { get; set; } = new();
    public List<SalesCenterBusinessRowViewModel> NeedsAttentionList { get; set; } = new();
}

public class SalesCenterBusinessRowViewModel
{
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string PublicMenuUrl { get; set; } = string.Empty;
    public int HealthScore { get; set; }
    public string HealthLabel { get; set; } = string.Empty;
    public string HealthBadgeClass { get; set; } = "bg-secondary";
    public int OrderCount { get; set; }
    public int ActiveProductCount { get; set; }
    public int CampaignCount { get; set; }
    public int NotificationCount { get; set; }
    public int AuditLogCount { get; set; }
    public bool IsDemoReady { get; set; }
    public string? AttentionReason { get; set; }
}
