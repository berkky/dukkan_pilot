namespace DukkanPilot.Web.Areas.Business.Models;

public class ProductReportViewModel
{
    public int TotalProductCount { get; set; }
    public int ActiveProductCount { get; set; }
    public string? TopSellingProductName { get; set; }
    public string? TopRevenueProductName { get; set; }
    public List<ProductSalesRowViewModel> TopProducts { get; set; } = new();
    public List<ProductSalesRowViewModel> ChartProducts { get; set; } = new();
}
