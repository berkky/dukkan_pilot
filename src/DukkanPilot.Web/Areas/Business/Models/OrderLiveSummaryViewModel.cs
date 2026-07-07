namespace DukkanPilot.Web.Areas.Business.Models;

public class OrderLiveSummaryViewModel
{
    public int PendingCount { get; set; }
    public int PreparingCount { get; set; }
    public int TodayOrderCount { get; set; }
    public decimal TodayRevenue { get; set; }
    public int? LatestOrderId { get; set; }
    public DateTime? LatestOrderCreatedAt { get; set; }
    public string? LatestOrderCustomerName { get; set; }
    public decimal? LatestOrderTotal { get; set; }
    public string? LatestOrderStatus { get; set; }
    public string? LatestOrderStatusText { get; set; }
    public string? LatestOrderStatusBadgeClass { get; set; }
    public DateTime ServerTime { get; set; }
}
