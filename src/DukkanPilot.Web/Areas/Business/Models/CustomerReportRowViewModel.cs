namespace DukkanPilot.Web.Areas.Business.Models;

public class CustomerReportRowViewModel
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalSpending { get; set; }
    public int TotalPoints { get; set; }
}
