using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Areas.Business.Models;

public class ReportCampaignImpactViewModel
{
    public int OrdersWithCampaign { get; set; }

    public int OrdersWithoutCampaign { get; set; }

    public decimal TotalDiscount { get; set; }

    public decimal CampaignOrderRevenue { get; set; }

    public decimal CampaignOrderSubtotal { get; set; }

    public decimal AverageCampaignBasket { get; set; }

    public decimal DiscountRatePercent { get; set; }

    public List<ReportCampaignPerformanceRowViewModel> TopCampaigns { get; set; } = [];
}

public class ReportCampaignPerformanceRowViewModel
{
    public int? CampaignId { get; set; }

    public string CampaignName { get; set; } = string.Empty;

    public int OrderCount { get; set; }

    public decimal TotalSubtotal { get; set; }

    public decimal TotalDiscount { get; set; }

    public decimal NetRevenue { get; set; }

    public decimal AverageBasket { get; set; }

    public decimal AverageDiscount { get; set; }

    public DateTime? LastUsedAt { get; set; }
}

public class CampaignReportIndexViewModel
{
    public string Period { get; set; } = "last7";

    public string PeriodLabel { get; set; } = "Son 7 Gün";

    public DateTime StartDateLocal { get; set; }

    public DateTime EndDateLocal { get; set; }

    public bool WasDateRangeAdjusted { get; set; }

    public ReportCampaignImpactViewModel Impact { get; set; } = new();

    public List<SalesReportOrderRowViewModel> RecentCampaignOrders { get; set; } = [];
}

public class CampaignPerformanceSummaryViewModel
{
    public int OrderCount { get; set; }

    public decimal TotalDiscount { get; set; }

    public decimal NetRevenue { get; set; }

    public decimal AverageBasket { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public List<CampaignPerformanceOrderRowViewModel> RecentOrders { get; set; } = [];
}

public class CampaignPerformanceOrderRowViewModel
{
    public int OrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string? CustomerName { get; set; }

    public decimal SubtotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; }
}
