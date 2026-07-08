namespace DukkanPilot.Web.Areas.Admin.Models;

public class SalesCenterViewModel
{
    public int TotalBusinesses { get; set; }
    public int ActiveBusinesses { get; set; }
    public int TrialBusinesses { get; set; }
    public int ExpiringSoonBusinesses { get; set; }
    public int DemoReadyBusinesses { get; set; }
    public int OnboardingReadyBusinesses { get; set; }
    public int HealthyBusinesses { get; set; }
    public int UpgradeOpportunityCount { get; set; }

    public decimal BillingOpenAmount { get; set; }
    public decimal BillingOverdueAmount { get; set; }
    public int BillingOverdueCount { get; set; }
    public decimal BillingPaidThisMonth { get; set; }
    public int WonWithoutInvoiceCount { get; set; }

    public List<SalesCenterBusinessRowViewModel> DemoReadyList { get; set; } = new();
    public List<SalesCenterBusinessRowViewModel> OnboardingReadyList { get; set; } = new();
    public List<SalesCenterBusinessRowViewModel> HealthyList { get; set; } = new();
    public List<SalesCenterBusinessRowViewModel> NeedsAttentionList { get; set; } = new();
    public List<SalesCenterWonHandoffViewModel> WonHandoffs { get; set; } = new();
}

public class SalesCenterWonHandoffViewModel
{
    public int SalesRequestId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string? BusinessName { get; set; }
    public int? BusinessId { get; set; }
    public int? OnboardingScore { get; set; }
    public string? OnboardingStatusLabel { get; set; }
    public string? OnboardingBadgeClass { get; set; }
    public int? SuccessScore { get; set; }
    public string? SuccessStatusLabel { get; set; }
    public string? SuccessBadgeClass { get; set; }
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
    public int? SuccessScore { get; set; }
    public string? SuccessLabel { get; set; }
    public string? SuccessBadgeClass { get; set; }
}
