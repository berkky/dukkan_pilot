namespace DukkanPilot.Web.Areas.Business.Models;

public class ProductSalesRowViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = "-";
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}
