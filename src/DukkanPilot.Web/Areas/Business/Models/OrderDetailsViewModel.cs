using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Areas.Business.Models;

public class OrderDetailsViewModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public OrderSource Source { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemViewModel> Items { get; set; } = new();
    public bool HasMatchingCustomer { get; set; }
    public bool HasActiveLoyaltyRule { get; set; }
    public int? PotentialLoyaltyPoints { get; set; }
    public int? AwardedLoyaltyPoints { get; set; }
    public string? ServiceType { get; set; }
    public string? TableLabelSnapshot { get; set; }
    public int? BusinessTableId { get; set; }

    public string ServiceTypeText => OrderDisplayHelper.GetServiceTypeLabel(ServiceType);
    public string ServiceTypeBadgeClass => OrderDisplayHelper.GetServiceTypeBadgeClass(ServiceType);
    public bool HasTableInfo => OrderDisplayHelper.HasTableInfo(ServiceType, TableLabelSnapshot);
    public string? TableDisplayLabel => OrderDisplayHelper.GetTableDisplayLabel(TableLabelSnapshot);
    public bool IsTableServiceOrder => ServiceType == Core.Common.OrderServiceTypes.TableService;
}
