namespace DukkanPilot.Web.Areas.Admin.Models;

public class BusinessListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public string PlanName { get; set; } = "-";
    public string SubscriptionStatusText { get; set; } = "-";
    public string SubscriptionStatusBadgeClass { get; set; } = "bg-secondary";
    public DateTime? SubscriptionEndDate { get; set; }
    public int ProductCount { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime CreatedAt { get; set; }

    public int HealthScore { get; set; }

    public string HealthLabel { get; set; } = string.Empty;

    public string HealthBadgeClass { get; set; } = "bg-secondary";

    public string PrimaryRiskReason { get; set; } = string.Empty;

    public string PrimaryRiskBadgeClass { get; set; } = "bg-secondary";

    public bool HasRisks { get; set; }
}
