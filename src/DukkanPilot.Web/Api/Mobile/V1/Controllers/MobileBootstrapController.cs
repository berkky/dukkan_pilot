using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Api.Mobile.V1.Authorization;
using DukkanPilot.Web.Api.Mobile.V1.Common;
using DukkanPilot.Web.Api.Mobile.V1.Contracts.Auth;
using DukkanPilot.Web.Api.Mobile.V1.Contracts.Bootstrap;
using DukkanPilot.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Api.Mobile.V1.Controllers;

[Route("api/mobile/v1/bootstrap")]
[Authorize(Policy = MobilePolicies.OwnerOrStaff)]
public sealed class MobileBootstrapController : MobileApiControllerBase
{
    private readonly AppDbContext _context;
    private readonly BusinessSubscriptionStatusHelper _subscriptionStatus;

    public MobileBootstrapController(
        AppDbContext context,
        BusinessSubscriptionStatusHelper subscriptionStatus)
    {
        _context = context;
        _subscriptionStatus = subscriptionStatus;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!MobilePrincipal.TryGetContext(User, out var mobileContext))
        {
            return MobileProblem(StatusCodes.Status401Unauthorized, "unauthorized", "Authentication is required.");
        }

        var membership = await _context.UserBusinessRoles
            .AsNoTracking()
            .Include(role => role.AppUser)
            .Include(role => role.Business)
            .SingleOrDefaultAsync(
                role => role.AppUserId == mobileContext.UserId &&
                        role.BusinessId == mobileContext.BusinessId &&
                        role.Role == mobileContext.BusinessRole &&
                        role.IsActive,
                cancellationToken);

        if (membership is null || !membership.AppUser.IsActive)
        {
            return MobileProblem(StatusCodes.Status401Unauthorized, "unauthorized", "The mobile session is no longer valid.");
        }

        if (!membership.Business.IsActive)
        {
            return MobileProblem(StatusCodes.Status403Forbidden, "business_inactive", "The business is inactive.");
        }

        var subscription = await _subscriptionStatus.GetStatusAsync(membership.BusinessId);
        if (!subscription.HasValidSubscription)
        {
            return MobileProblem(StatusCodes.Status403Forbidden, "forbidden", "An active subscription is required.");
        }

        var modules = membership.Role == BusinessRole.Owner
            ? new[] { "dashboard", "orders", "kitchen", "business", "staff", "billing" }
            : new[] { "dashboard", "orders", "kitchen" };

        return Ok(new MobileBootstrapResponse(
            new MobileUserSummary(
                membership.AppUser.Id,
                membership.AppUser.FullName,
                membership.AppUser.Email,
                membership.AppUser.Role.ToString()),
            new MobileBusinessSummary(
                membership.Business.Id,
                membership.Business.Name,
                membership.Role.ToString()),
            membership.Role.ToString(),
            MobilePermissions.For(membership.Role),
            new MobilePlanSummary(
                subscription.PlanName,
                subscription.StatusText,
                subscription.EndDate,
                subscription.HasValidSubscription),
            modules,
            DateTime.UtcNow));
    }
}
