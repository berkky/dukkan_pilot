using DukkanPilot.Mobile.Core.Api;
using DukkanPilot.Mobile.Core.Contracts;
using DukkanPilot.Mobile.Core.Security;
using DukkanPilot.Mobile.Core.State;

namespace DukkanPilot.Mobile.Core.Session;

public sealed class MobileSessionService : IMobileSessionService, IDisposable
{
    private readonly IMobileApiClient _apiClient;
    private readonly MobileTokenManager _tokenManager;
    private readonly ISecureTokenStore _tokenStore;
    private readonly SessionState _session;
    private readonly BootstrapState _bootstrap;
    private PendingLogin? _pendingLogin;

    public MobileSessionService(
        IMobileApiClient apiClient,
        MobileTokenManager tokenManager,
        ISecureTokenStore tokenStore,
        SessionState session,
        BootstrapState bootstrap)
    {
        _apiClient = apiClient;
        _tokenManager = tokenManager;
        _tokenStore = tokenStore;
        _session = session;
        _bootstrap = bootstrap;
    }

    public IReadOnlyList<MobileBusinessOption> BusinessOptions =>
        _pendingLogin?.Businesses ?? [];

    public async Task<LoginOutcome> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        ClearPendingLogin();
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(password))
        {
            const string message = "E-posta ve şifre alanları zorunludur.";
            _session.SetError(message);
            return LoginOutcome.Failed(message, "validation_failed");
        }

        _session.SetBusy(true);
        _session.SetError(null);
        try
        {
            var response = await _apiClient.LoginAsync(
                new MobileLoginRequest
                {
                    Email = normalizedEmail,
                    Password = password,
                    BusinessId = null
                },
                cancellationToken);
            await CompleteAuthenticationAsync(response, cancellationToken);
            return LoginOutcome.Authenticated();
        }
        catch (MobileApiException exception)
            when (exception.Code == "business_selection_required" &&
                  exception.Businesses.Count > 0)
        {
            _pendingLogin = new PendingLogin(
                normalizedEmail,
                password.ToCharArray(),
                exception.Businesses);
            return LoginOutcome.Selection(exception.Businesses);
        }
        catch (MobileApiException exception)
        {
            _session.SetError(exception.UserMessage);
            return LoginOutcome.Failed(
                exception.UserMessage,
                exception.Code,
                exception.TraceId);
        }
        finally
        {
            _session.SetBusy(false);
        }
    }

    public async Task<LoginOutcome> SelectBusinessAsync(
        int businessId,
        CancellationToken cancellationToken = default)
    {
        var pending = _pendingLogin;
        if (pending is null ||
            pending.Businesses.All(business => business.Id != businessId))
        {
            const string message = "Seçilen işletme bu giriş isteğinde bulunmuyor.";
            return LoginOutcome.Failed(message, "invalid_business");
        }

        _session.SetBusy(true);
        try
        {
            var password = new string(pending.Password);
            var response = await _apiClient.LoginAsync(
                new MobileLoginRequest
                {
                    Email = pending.Email,
                    Password = password,
                    BusinessId = businessId
                },
                cancellationToken);
            await CompleteAuthenticationAsync(response, cancellationToken);
            return LoginOutcome.Authenticated();
        }
        catch (MobileApiException exception)
        {
            _session.SetError(exception.UserMessage);
            return LoginOutcome.Failed(
                exception.UserMessage,
                exception.Code,
                exception.TraceId);
        }
        finally
        {
            ClearPendingLogin();
            _session.SetBusy(false);
        }
    }

    public async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        _session.BeginRestore();
        try
        {
            if (!await _tokenManager.TryRefreshAsync(null, cancellationToken))
            {
                _bootstrap.Clear();
                return;
            }

            await LoadBootstrapAsync(cancellationToken);
        }
        catch
        {
            await SafeClearAsync(cancellationToken);
        }
        finally
        {
            _session.CompleteRestore();
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = await _tokenStore.ReadAsync(cancellationToken);
            if (_session.IsAuthenticated && stored is not null)
            {
                await _apiClient.LogoutAsync(stored.RefreshToken, cancellationToken);
            }
        }
        catch (MobileApiException)
        {
            // Local logout remains authoritative when the server cannot be reached.
        }
        finally
        {
            await SafeClearAsync(cancellationToken);
        }
    }

    public async Task LogoutAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_session.IsAuthenticated)
            {
                await _apiClient.LogoutAllAsync(cancellationToken);
            }
        }
        catch (MobileApiException)
        {
            // Never leave local credentials behind after an explicit logout-all.
        }
        finally
        {
            await SafeClearAsync(cancellationToken);
        }
    }

    public void Dispose() => ClearPendingLogin();

    private async Task CompleteAuthenticationAsync(
        MobileAuthResponse response,
        CancellationToken cancellationToken)
    {
        await _tokenManager.ApplyAsync(response, cancellationToken);
        try
        {
            await LoadBootstrapAsync(cancellationToken);
        }
        catch
        {
            await SafeClearAsync(cancellationToken);
            throw;
        }
    }

    private async Task LoadBootstrapAsync(CancellationToken cancellationToken)
    {
        _bootstrap.SetBusy(true);
        try
        {
            var response = await _apiClient.GetBootstrapAsync(cancellationToken);
            _session.ApplyBootstrap(response);
            _bootstrap.Apply(response);
        }
        catch (MobileApiException exception)
        {
            _bootstrap.SetError(exception.UserMessage);
            throw;
        }
    }

    private async Task SafeClearAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _tokenManager.ClearAsync(cancellationToken);
        }
        catch
        {
            _session.Clear();
        }

        _bootstrap.Clear();
        ClearPendingLogin();
    }

    private void ClearPendingLogin()
    {
        if (_pendingLogin is not null)
        {
            Array.Clear(_pendingLogin.Password);
            _pendingLogin = null;
        }
    }

    private sealed record PendingLogin(
        string Email,
        char[] Password,
        IReadOnlyList<MobileBusinessOption> Businesses);
}
