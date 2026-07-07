using Microsoft.AspNetCore.Mvc.Rendering;

namespace DukkanPilot.Web.Areas.Business.Models;

public class ProductsIndexViewModel
{
    public int TotalProducts { get; set; }

    public int ActiveProducts { get; set; }

    public int PassiveProducts { get; set; }

    public decimal AveragePrice { get; set; }

    public PlanUsageMetricViewModel ProductPlanUsage { get; set; } = new() { Name = "Ürünler" };

    public int? CategoryFilter { get; set; }

    public string StatusFilter { get; set; } = "all";

    public string? Search { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public List<SelectListItem> AvailableCategories { get; set; } = [];

    public List<ProductIndexRowViewModel> Products { get; set; } = [];

    public string ExportCsvUrl { get; set; } = "/Business/Products/ExportCsv";
}

public class ProductIndexRowViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public bool CategoryIsActive { get; set; }

    public decimal Price { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public bool IsPublicVisible { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}
