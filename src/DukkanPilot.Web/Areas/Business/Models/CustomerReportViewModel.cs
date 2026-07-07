namespace DukkanPilot.Web.Areas.Business.Models;

public class CustomerReportViewModel
{
    public int TotalCustomerCount { get; set; }
    public int ActiveCustomerCount { get; set; }
    public string? TopOrderCustomerName { get; set; }
    public string? TopSpendingCustomerName { get; set; }
    public string? TopPointsCustomerName { get; set; }
    public List<CustomerReportRowViewModel> TopCustomers { get; set; } = new();
}
