using System.Security.Claims;
using DukkanPilot.Core.Enums;
using DukkanPilot.Web.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Area("Business")]
[Authorize(Roles = $"{nameof(UserRole.BusinessOwner)},{nameof(UserRole.Staff)}")]
public abstract class BusinessBaseController : Controller
{
    protected int? CurrentBusinessId => GetCurrentBusinessId();

    protected BusinessRole? CurrentBusinessRole
    {
        get
        {
            var claimValue = User.FindFirst(AuthClaimTypes.BusinessRole)?.Value;
            return Enum.TryParse<BusinessRole>(claimValue, out var role) ? role : null;
        }
    }

    protected int? CurrentUserId
    {
        get
        {
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claimValue, out var userId) && userId > 0 ? userId : null;
        }
    }

    protected string? CurrentUserEmail => User.FindFirst(ClaimTypes.Email)?.Value;

    protected int? GetCurrentBusinessId()
    {
        var claimValue = User.FindFirst(AuthClaimTypes.BusinessId)?.Value;
        if (!int.TryParse(claimValue, out var businessId) || businessId <= 0)
        {
            return null;
        }

        return businessId;
    }

    protected IActionResult? GetCurrentBusinessIdOrForbid(out int businessId)
    {
        var currentBusinessId = GetCurrentBusinessId();
        if (currentBusinessId is null)
        {
            businessId = 0;
            return Forbid();
        }

        businessId = currentBusinessId.Value;
        return null;
    }
}
