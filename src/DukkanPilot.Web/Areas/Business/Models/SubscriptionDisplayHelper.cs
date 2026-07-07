using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Areas.Business.Models;

public static class SubscriptionDisplayHelper
{
    public static string GetStatusLabel(SubscriptionStatus status) => status switch
    {
        SubscriptionStatus.Trial => "Deneme",
        SubscriptionStatus.Active => "Aktif",
        SubscriptionStatus.Expired => "Süresi Dolmuş",
        SubscriptionStatus.Cancelled => "İptal",
        _ => status.ToString()
    };

    public static string GetStatusBadgeClass(SubscriptionStatus status) => status switch
    {
        SubscriptionStatus.Trial => "bg-warning text-dark",
        SubscriptionStatus.Active => "bg-success",
        SubscriptionStatus.Expired => "bg-danger",
        SubscriptionStatus.Cancelled => "bg-secondary",
        _ => "bg-secondary"
    };
}
