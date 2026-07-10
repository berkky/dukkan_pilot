using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Areas.Business.Models;

public class OrderListViewModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public OrderSource Source { get; set; }
    public string? ServiceType { get; set; }
    public string? TableLabelSnapshot { get; set; }
    public DateTime CreatedAt { get; set; }

    public string ServiceTypeText => OrderDisplayHelper.GetServiceTypeLabel(ServiceType);
    public string ServiceTypeBadgeClass => OrderDisplayHelper.GetServiceTypeBadgeClass(ServiceType);
    public bool HasTableInfo => OrderDisplayHelper.HasTableInfo(ServiceType, TableLabelSnapshot);
    public string? TableDisplayLabel => OrderDisplayHelper.GetTableDisplayLabel(TableLabelSnapshot);
}
