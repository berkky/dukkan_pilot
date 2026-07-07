namespace DukkanPilot.Web.Areas.Business.Models;

public class OrderSummaryViewModel
{
    public int TodayOrderCount { get; set; }
    public int PendingCount { get; set; }
    public int PreparingCount { get; set; }
    public decimal TodayRevenue { get; set; }
    public int? LatestOrderId { get; set; }
    public DateTime? LatestOrderCreatedAt { get; set; }
}

public class OrderIndexViewModel
{
    public OrderSummaryViewModel Summary { get; set; } = new();
    public List<OrderListViewModel> Orders { get; set; } = new();
    public string? StatusFilter { get; set; }
    public string PeriodFilter { get; set; } = "all";
    public string? Search { get; set; }
}

public class DashboardOrderSummaryViewModel
{
    public int TodayOrderCount { get; set; }
    public int PendingCount { get; set; }
    public int PreparingCount { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal Last7DaysRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal AverageOrderAmount { get; set; }
    public int? LatestOrderId { get; set; }
    public DateTime? LatestOrderCreatedAt { get; set; }
}

public class DashboardStatusDistributionViewModel
{
    public int PendingCount { get; set; }
    public int PreparingCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }

    public int TotalCount => PendingCount + PreparingCount + CompletedCount + CancelledCount;
}
