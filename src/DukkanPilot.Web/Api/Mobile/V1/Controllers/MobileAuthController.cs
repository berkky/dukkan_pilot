using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Infrastructure.Security;
using DukkanPilot.Web.Api.Mobile.V1.Authorization;
using DukkanPilot.Web.Api.Mobile.V1.Common;
using DukkanPilot.Web.Api.Mobile.V1.Contracts.Auth;
using DukkanPilot.Web.Api.Mobile.V1.Services;
using DukkanPilot.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Api.Mobile.V1.Controllers;

[Route("api/mobile/v1/auth")]
public sealed class MobileAuthController : MobileApiControllerBase
{
    private static readonly string DummyPasswordHash = PasswordHelper.HashPassword("mobile-auth-dummy-password");
    private readonly AppDbContext _context;
    private readonly IMobileTokenService _tokenService;
    private readonly BusinessSubscriptionStatusHelper _subscriptionStatus;

    public MobileAuthController(
        AppDbContext context,
        IMobileTokenService tokenService,
        BusinessSubscriptionStatusHelper subscriptionStatus)
    {
        _context = context;
        _tokenService = tokenService;
        _subscriptionStatus = subscriptionStatus;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("mobile-login")]
    public async Task<IActionResult> Login(
        MobileLoginRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.AppUsers
            .Include(candidate => candidate.BusinessRoles)
            .ThenInclude(membership => membership.Business)
            .SingleOrDefaultAsync(
                candidate => candidate.Email.ToLower() == normalizedEmail,
                cancellationToken);

        var passwordHash = user?.PasswordHash ?? DummyPasswordHash;
        var passwordValid = PasswordHelper.VerifyPassword(request.Password, passwordHash);
        if (user is null || !passwordValid)
        {
            return InvalidCredentials();
        }

        if (!user.IsActive || user.Role is not (UserRole.BusinessOwner or UserRole.Staff))
        {
            return MobileProblem(
                StatusCodes.Status403Forbidden,
                "account_inactive",
                "The account cannot use the mobile API.");
        }

        var memberships = user.BusinessRoles
            .Where(membership => membership.IsActive &&
                                 membership.Role is BusinessRole.Owner or BusinessRole.Staff)
            .OrderByDescending(membership => membership.Role == BusinessRole.Owner)
            .ThenBy(membership => membership.Business.Name)
            .ToList();

        UserBusinessRole? selectedMembership;
        if (request.BusinessId.HasValue)
        {
            selectedMembership = memberships.SingleOrDefault(
                membership => membership.BusinessId == request.BusinessId.Value);
            if (selectedMembership is null)
            {
                return MobileProblem(
                    StatusCodes.Status400BadRequest,
                    "invalid_business",
                    "The selected business is not available for this account.");
            }
        }
        else
        {
            var activeMemberships = memberships.Where(membership => membership.Business.IsActive).ToList();
            if (activeMemberships.Count > 1)
            {
                var options = activeMemberships
                    .Select(membership => new MobileBusinessOption(
                        membership.BusinessId,
                        membership.Business.Name,
                        membership.Role.ToString()))
                    .ToArray();

                return MobileProblem(
                    StatusCodes.Status409Conflict,
                    "business_selection_required",
                    "A business must be selected.",
                    extensions: new Dictionary<string, object?> { ["businesses"] = options });
            }

            selectedMembership = activeMemberships.SingleOrDefault() ?? memberships.SingleOrDefault();
            if (selectedMembership is null)
            {
                return MobileProblem(
                    StatusCodes.Status403Forbidden,
                    "invalid_business",
                    "No active business membership is available.");
            }
        }

        if (!selectedMembership.Business.IsActive)
        {
            return MobileProblem(
                StatusCodes.Status403Forbidden,
                "business_inactive",
                "The selected business is inactive.");
        }

        if (!await _subscriptionStatus.HasValidSubscriptionAsync(selectedMembership.BusinessId))
        {
            return MobileProblem(
                StatusCodes.Status403Forbidden,
                "forbidden",
                "The selected business does not have an active subscription.");
        }

        var pair = await _tokenService.IssueAsync(
            user,
            selectedMembership.Business,
            selectedMembership.Role,
            cancellationToken);

        return Ok(MapResponse(pair, user, selectedMembership.Business, selectedMembership.Role));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("mobile-refresh")]
    public async Task<IActionResult> Refresh(
        MobileRefreshRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tokenService.RefreshAsync(request.RefreshToken, cancellationToken);
        if (result.Succeeded)
        {
            return Ok(MapResponse(
                result.TokenPair!,
                result.User!,
                result.Business!,
                result.BusinessRole!.Value));
        }

        return result.Failure switch
        {
            MobileRefreshFailure.Expired => MobileProblem(
                StatusCodes.Status401Unauthorized,
                "refresh_token_expired",
                "The refresh token has expired."),
            MobileRefreshFailure.Reused => MobileProblem(
                StatusCodes.Status401Unauthorized,
                "refresh_token_reused",
                "Refresh token reuse was detected and the token family was revoked."),
            MobileRefreshFailure.AccessDenied => MobileProblem(
                StatusCodes.Status403Forbidden,
                "forbidden",
                "Mobile access is no longer available."),
            _ => MobileProblem(
                StatusCodes.Status401Unauthorized,
                "invalid_refresh_token",
                "The refresh token is invalid.")
        };
    }

    [HttpPost("logout")]
    [Authorize(Policy = MobilePolicies.Authenticated)]
    public async Task<IActionResult> Logout(
        MobileLogoutRequest request,
        CancellationToken cancellationToken)
    {
        if (!MobilePrincipal.TryGetContext(User, out var mobileContext))
        {
            return MobileProblem(StatusCodes.Status401Unauthorized, "unauthorized", "Authentication is required.");
        }

        await _tokenService.LogoutAsync(
            request.RefreshToken,
            mobileContext.UserId,
            mobileContext.BusinessId,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("logout-all")]
    [Authorize(Policy = MobilePolicies.Authenticated)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        if (!MobilePrincipal.TryGetContext(User, out var mobileContext))
        {
            return MobileProblem(StatusCodes.Status401Unauthorized, "unauthorized", "Authentication is required.");
        }

        await _tokenService.LogoutAllAsync(
            mobileContext.UserId,
            mobileContext.BusinessId,
            cancellationToken);

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize(Policy = MobilePolicies.Authenticated)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
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

        if (!membership.Business.IsActive ||
            !await _subscriptionStatus.HasValidSubscriptionAsync(membership.BusinessId))
        {
            return MobileProblem(StatusCodes.Status403Forbidden, "forbidden", "Business access is no longer available.");
        }

        return Ok(new
        {
            User = new MobileUserSummary(
                membership.AppUser.Id,
                membership.AppUser.FullName,
                membership.AppUser.Email,
                membership.AppUser.Role.ToString()),
            Business = new MobileBusinessSummary(
                membership.Business.Id,
                membership.Business.Name,
                membership.Role.ToString()),
            Permissions = MobilePermissions.For(membership.Role)
        });
    }

    private ObjectResult InvalidCredentials()
    {
        return MobileProblem(
            StatusCodes.Status401Unauthorized,
            "invalid_credentials",
            "Invalid email or password.");
    }

    private static MobileAuthResponse MapResponse(
        MobileTokenPair pair,
        AppUser user,
        Business business,
        BusinessRole businessRole)
    {
        return new MobileAuthResponse(
            pair.AccessToken,
            pair.RefreshToken,
            pair.AccessTokenExpiresAtUtc,
            pair.RefreshTokenExpiresAtUtc,
            new MobileUserSummary(user.Id, user.FullName, user.Email, user.Role.ToString()),
            new MobileBusinessSummary(business.Id, business.Name, businessRole.ToString()),
            MobilePermissions.For(businessRole));
    }
}
