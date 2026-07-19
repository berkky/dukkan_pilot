using DukkanPilot.Mobile.Core.Api;
using DukkanPilot.Mobile.Core.Contracts;
using DukkanPilot.Mobile.Core.State;

namespace DukkanPilot.Mobile.Core.Security;

public sealed class MobileTokenManager
{
    private readonly SessionState _session;
    private readonly ISecureTokenStore _tokenStore;
    private readonly IMobileApiClient _refreshClient;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public MobileTokenManager(
        SessionState session,
        ISecureTokenStore tokenStore,
        IMobileApiClient refreshClient)
    {
        _session = session;
        _tokenStore = tokenStore;
        _refreshClient = refreshClient;
    }

    public async Task ApplyAsync(
        MobileAuthResponse response,
        CancellationToken cancellationToken = default)
    {
        await _tokenStore.SaveAsync(
            new SecureTokenRecord(
                response.RefreshToken,
                response.RefreshTokenExpiresAtUtc),
            cancellationToken);
        _session.ApplyAuthentication(response);
    }

    public async Task<bool> TryRefreshAsync(
        string? accessTokenObservedByRequest,
        CancellationToken cancellationToken = default)
    {
        if (TokenWasAlreadyRotated(accessTokenObservedByRequest))
        {
            return true;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (TokenWasAlreadyRotated(accessTokenObservedByRequest))
            {
                return true;
            }

            var stored = await _tokenStore.ReadAsync(cancellationToken);
            if (stored is null ||
                string.IsNullOrWhiteSpace(stored.RefreshToken) ||
                stored.RefreshTokenExpiresAtUtc <= DateTime.UtcNow)
            {
                await ClearAsync(cancellationToken);
                return false;
            }

            try
            {
                var response = await _refreshClient.RefreshAsync(
                    stored.RefreshToken,
                    cancellationToken);
                await ApplyAsync(response, cancellationToken);
                return true;
            }
            catch (MobileApiException)
            {
                await ClearAsync(cancellationToken);
                return false;
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _tokenStore.ClearAsync(cancellationToken);
        _session.Clear();
    }

    private bool TokenWasAlreadyRotated(string? observed)
    {
        return observed is not null &&
               _session.IsAuthenticated &&
               !string.Equals(observed, _session.AccessToken, StringComparison.Ordinal);
    }
}
