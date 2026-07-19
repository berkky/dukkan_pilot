using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DukkanPilot.Core.Enums;
using DukkanPilot.Web.Constants;

namespace DukkanPilot.Web.Api.Mobile.V1.Authorization;

public static class MobileAuthDefaults
{
    public const string Scheme = "MobileBearer";
    public const string ClientIdClaim = "client_id";
    public const string ClientId = "dukkanpilot-mobile";
}

public static class MobilePolicies
{
    public const string Authenticated = "MobileAuthenticated";
    public const string OwnerOrStaff = "MobileOwnerOrStaff";
    public const string OwnerOnly = "MobileOwnerOnly";
}

public readonly record struct MobileRequestContext(int UserId, int BusinessId, BusinessRole BusinessRole);

public static class MobilePrincipal
{
    public static bool TryGetContext(ClaimsPrincipal principal, out MobileRequestContext context)
    {
        context = default;
        var userIdValue = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var businessIdValue = principal.FindFirst(AuthClaimTypes.BusinessId)?.Value;
        var businessRoleValue = principal.FindFirst(AuthClaimTypes.BusinessRole)?.Value;

        return int.TryParse(userIdValue, out var userId) && userId > 0 &&
               int.TryParse(businessIdValue, out var businessId) && businessId > 0 &&
               Enum.TryParse<BusinessRole>(businessRoleValue, ignoreCase: false, out var businessRole) &&
               Enum.IsDefined(businessRole) &&
               Assign(userId, businessId, businessRole, out context);
    }

    private static bool Assign(int userId, int businessId, BusinessRole role, out MobileRequestContext context)
    {
        context = new MobileRequestContext(userId, businessId, role);
        return true;
    }
}

public static class MobilePermissions
{
    private static readonly string[] StaffPermissions =
    [
        "orders.read",
        "orders.status.update",
        "kitchen.read",
        "dashboard.read"
    ];

    private static readonly string[] OwnerPermissions =
    [
        .. StaffPermissions,
        "business.manage",
        "staff.manage",
        "billing.read"
    ];

    public static IReadOnlyList<string> For(BusinessRole role) => role switch
    {
        BusinessRole.Owner => OwnerPermissions,
        BusinessRole.Staff => StaffPermissions,
        _ => Array.Empty<string>()
    };
}
