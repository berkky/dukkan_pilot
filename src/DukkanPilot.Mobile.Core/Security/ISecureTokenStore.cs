namespace DukkanPilot.Mobile.Core.Security;

public sealed record SecureTokenRecord(
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

public interface ISecureTokenStore
{
    Task<SecureTokenRecord?> ReadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(SecureTokenRecord token, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}
