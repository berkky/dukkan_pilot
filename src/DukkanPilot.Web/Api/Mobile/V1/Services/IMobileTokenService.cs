using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Api.Mobile.V1.Services;

public sealed record MobileTokenPair(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc);

public enum MobileRefreshFailure
{
    None,
    Invalid,
    Expired,
    Reused,
    AccessDenied
}

public sealed record MobileRefreshResult(
    MobileRefreshFailure Failure,
    MobileTokenPair? TokenPair = null,
    AppUser? User = null,
    Business? Business = null,
    BusinessRole? BusinessRole = null)
{
    public bool Succeeded => Failure == MobileRefreshFailure.None && TokenPair is not null;
}

public interface IMobileTokenService
{
    Task<MobileTokenPair> IssueAsync(
        AppUser user,
        Business business,
        BusinessRole businessRole,
        CancellationToken cancellationToken);

    Task<MobileRefreshResult> RefreshAsync(string rawRefreshToken, CancellationToken cancellationToken);

    Task LogoutAsync(
        string rawRefreshToken,
        int userId,
        int businessId,
        CancellationToken cancellationToken);

    Task<int> LogoutAllAsync(int userId, int businessId, CancellationToken cancellationToken);
}
