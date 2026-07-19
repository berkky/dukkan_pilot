using System.Globalization;
using DukkanPilot.Mobile.Core.Api;
using DukkanPilot.Mobile.Core.Security;

namespace DukkanPilot.Mobile.Services;

public sealed class MauiSecureTokenStore : ISecureTokenStore
{
    private const string RefreshTokenKey = "dukkanpilot.refresh_token";
    private const string RefreshExpiryKey = "dukkanpilot.refresh_expiry_utc";
    private readonly ISecureStorage _secureStorage;

    public MauiSecureTokenStore(ISecureStorage secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public async Task<SecureTokenRecord?> ReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var refreshToken = await _secureStorage.GetAsync(RefreshTokenKey);
            var expiryText = await _secureStorage.GetAsync(RefreshExpiryKey);
            if (string.IsNullOrWhiteSpace(refreshToken) ||
                !DateTime.TryParse(
                    expiryText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var expiry))
            {
                await ClearAsync(cancellationToken);
                return null;
            }

            return new SecureTokenRecord(refreshToken, expiry.ToUniversalTime());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await ClearAsync(cancellationToken);
            return null;
        }
    }

    public async Task SaveAsync(
        SecureTokenRecord token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _secureStorage.SetAsync(RefreshTokenKey, token.RefreshToken);
            await _secureStorage.SetAsync(
                RefreshExpiryKey,
                token.RefreshTokenExpiresAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            await ClearAsync(cancellationToken);
            throw new MobileApiException(
                "secure_storage_error",
                "Güvenli oturum bilgisi saklanamadı. Lütfen yeniden giriş yapın.",
                innerException: exception);
        }
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            _secureStorage.Remove(RefreshTokenKey);
            _secureStorage.Remove(RefreshExpiryKey);
        }
        catch
        {
            // A damaged platform store must never prevent local session cleanup.
        }

        return Task.CompletedTask;
    }
}
