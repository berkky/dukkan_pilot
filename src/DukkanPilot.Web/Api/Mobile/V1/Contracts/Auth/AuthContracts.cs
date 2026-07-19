using System.ComponentModel.DataAnnotations;

namespace DukkanPilot.Web.Api.Mobile.V1.Contracts.Auth;

public sealed class MobileLoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public int? BusinessId { get; set; }
}

public sealed class MobileRefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class MobileLogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed record MobileUserSummary(int Id, string FullName, string Email, string Role);
public sealed record MobileBusinessSummary(int Id, string Name, string Role);
public sealed record MobileBusinessOption(int Id, string Name, string Role);

public sealed record MobileAuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc,
    MobileUserSummary User,
    MobileBusinessSummary Business,
    IReadOnlyList<string> Permissions);
