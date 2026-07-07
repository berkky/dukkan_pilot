using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Areas.Business.Models;

public static class StaffDisplayHelper
{
    public static string GetRoleLabel(BusinessRole role) => role switch
    {
        BusinessRole.Owner => "İşletme Sahibi",
        BusinessRole.Staff => "Personel",
        _ => role.ToString()
    };

    public static string GetRoleBadgeClass(BusinessRole role) => role switch
    {
        BusinessRole.Owner => "bg-primary",
        BusinessRole.Staff => "bg-info",
        _ => "bg-secondary"
    };
}
