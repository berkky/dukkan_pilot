using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Areas.Business.Models;

public static class LoyaltyDisplayHelper
{
    public static string GetTypeLabel(LoyaltyTransactionType type) => type switch
    {
        LoyaltyTransactionType.Earn => "Kazanım",
        LoyaltyTransactionType.Redeem => "Kullanım",
        LoyaltyTransactionType.ManualAdjust => "Manuel",
        _ => type.ToString()
    };

    public static string GetTypeBadgeClass(LoyaltyTransactionType type) => type switch
    {
        LoyaltyTransactionType.Earn => "bg-success",
        LoyaltyTransactionType.Redeem => "bg-danger",
        LoyaltyTransactionType.ManualAdjust => "bg-secondary",
        _ => "bg-secondary"
    };
}
