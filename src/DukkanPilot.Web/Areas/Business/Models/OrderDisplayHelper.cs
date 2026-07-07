using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Areas.Business.Models;

public static class OrderDisplayHelper
{
    public static string GetStatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Beklemede",
        OrderStatus.Preparing => "Hazırlanıyor",
        OrderStatus.Completed => "Tamamlandı",
        OrderStatus.Cancelled => "İptal",
        _ => status.ToString()
    };

    public static string GetStatusBadgeClass(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "bg-warning text-dark",
        OrderStatus.Preparing => "bg-info text-dark",
        OrderStatus.Completed => "bg-success",
        OrderStatus.Cancelled => "bg-secondary",
        _ => "bg-secondary"
    };

    public static string GetSourceLabel(OrderSource source) => source switch
    {
        OrderSource.WhatsApp => "WhatsApp",
        OrderSource.Manual => "Manuel",
        OrderSource.Other => "Diğer",
        _ => source.ToString()
    };

    public static string GetSourceBadgeClass(OrderSource source) => source switch
    {
        OrderSource.WhatsApp => "bg-success",
        OrderSource.Manual => "bg-primary",
        OrderSource.Other => "bg-secondary",
        _ => "bg-secondary"
    };
}
