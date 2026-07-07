namespace DukkanPilot.Web.Areas.Business.Models;

public class MenuStudioViewModel
{
    public string BusinessName { get; set; } = string.Empty;

    public string BusinessSlug { get; set; } = string.Empty;

    public string PublicMenuUrl { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }

    public string? Description { get; set; }

    public string? ThemeColor { get; set; }

    public bool IsBusinessOwner { get; set; }

    public bool HasBusinessName { get; set; }

    public bool HasBusinessDescription { get; set; }

    public bool HasLogo { get; set; }

    public bool HasWhatsAppNumber { get; set; }

    public bool HasCategory { get; set; }

    public bool HasActiveProduct { get; set; }

    public bool HasPublicMenuLink { get; set; }

    public bool HasQrActions { get; set; } = true;

    public int TotalCategories { get; set; }

    public int ActiveCategories { get; set; }

    public int TotalProducts { get; set; }

    public int ActiveProducts { get; set; }

    public int PassiveProducts { get; set; }

    public decimal AverageProductPrice { get; set; }

    public decimal MaxProductPrice { get; set; }

    public decimal MinProductPrice { get; set; }

    public PlanUsageMetricViewModel ProductPlanUsage { get; set; } = new() { Name = "Ürünler" };

    public List<MenuHealthCheckItemViewModel> HealthChecks { get; set; } = [];

    public List<MenuStudioCategorySummaryViewModel> CategorySummaries { get; set; } = [];
}

public class MenuHealthCheckItemViewModel
{
    public string Label { get; set; } = string.Empty;

    public bool IsComplete { get; set; }

    public string? ActionLabel { get; set; }

    public string? ActionUrl { get; set; }
}

public class MenuStudioCategorySummaryViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int ProductCount { get; set; }

    public int ActiveProductCount { get; set; }

    public decimal AveragePrice { get; set; }

    public bool IsPublicVisible { get; set; }
}
