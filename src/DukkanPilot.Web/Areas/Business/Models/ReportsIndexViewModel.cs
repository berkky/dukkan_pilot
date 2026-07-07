namespace DukkanPilot.Web.Areas.Business.Models;

public class ReportsIndexViewModel
{
    public string Period { get; set; } = "last7";

    public string PeriodLabel { get; set; } = "Son 7 Gün";

    public DateTime StartDateLocal { get; set; }

    public DateTime EndDateLocal { get; set; }

    public bool WasDateRangeAdjusted { get; set; }

    public ReportKpiViewModel Kpis { get; set; } = new();

    public List<ReportDailyRevenueViewModel> DailyPerformance { get; set; } = [];

    public List<ReportTopProductViewModel> TopProducts { get; set; } = [];

    public DashboardStatusDistributionViewModel StatusDistribution { get; set; } = new();

    public List<SalesReportOrderRowViewModel> RecentOrders { get; set; } = [];
}

public class ReportKpiViewModel
{
    public decimal TotalRevenue { get; set; }

    public int TotalOrders { get; set; }

    public int CompletedOrders { get; set; }

    public int PendingOrders { get; set; }

    public int PreparingOrders { get; set; }

    public int CancelledOrders { get; set; }

    public decimal AverageBasket { get; set; }

    public decimal MaxOrderAmount { get; set; }

    public decimal MinOrderAmount { get; set; }
}

public class ReportDailyRevenueViewModel
{
    public DateTime Date { get; set; }

    public string DateLabel { get; set; } = string.Empty;

    public int OrderCount { get; set; }

    public decimal Revenue { get; set; }

    public decimal AverageBasket { get; set; }

    public int RevenueBarPercent { get; set; }
}

public class ReportTopProductViewModel
{
    public string ProductName { get; set; } = string.Empty;

    public int QuantitySold { get; set; }

    public decimal Revenue { get; set; }

    public int OrderCount { get; set; }
}
