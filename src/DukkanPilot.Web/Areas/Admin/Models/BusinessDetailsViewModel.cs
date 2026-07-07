namespace DukkanPilot.Web.Areas.Admin.Models;

public class BusinessDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string? WhatsAppNumber { get; set; }
    public string ThemeColor { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;

    public string ActivePlanName { get; set; } = "-";
    public string SubscriptionStatus { get; set; } = "-";
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }

    public int CategoryCount { get; set; }
    public int ProductCount { get; set; }
    public int CustomerCount { get; set; }
    public int OrderCount { get; set; }
}
