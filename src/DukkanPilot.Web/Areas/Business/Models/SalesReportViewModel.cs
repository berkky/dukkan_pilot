namespace DukkanPilot.Web.Areas.Business.Models;

public class SalesReportViewModel
{
    public decimal TodayRevenue { get; set; }
    public decimal Last7DaysRevenue { get; set; }
    public decimal Last30DaysRevenue { get; set; }
    public decimal AverageOrderAmount { get; set; }
    public int CompletedOrderCount { get; set; }
    public int PendingOrderCount { get; set; }
    public int CancelledOrderCount { get; set; }
    public List<SalesReportOrderRowViewModel> RecentOrders { get; set; } = new();
    public List<SalesDailyChartItemViewModel> DailyChart { get; set; } = new();
}
