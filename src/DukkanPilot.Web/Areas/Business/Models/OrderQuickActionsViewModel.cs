using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Areas.Business.Models;

public class OrderQuickActionsViewModel
{
    public int OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public string? StatusFilter { get; set; }
    public string PeriodFilter { get; set; } = "all";
    public string? Search { get; set; }
    public string ReturnTo { get; set; } = "details";
}
