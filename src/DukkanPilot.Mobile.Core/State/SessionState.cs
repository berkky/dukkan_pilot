using DukkanPilot.Mobile.Core.Contracts;

namespace DukkanPilot.Mobile.Core.State;

public interface IAccessTokenProvider
{
    string? AccessToken { get; }
}

public sealed class SessionState : IAccessTokenProvider
{
    public event Action? Changed;

    public MobileUserSummary? CurrentUser { get; private set; }
    public MobileBusinessSummary? CurrentBusiness { get; private set; }
    public string? BusinessRole { get; private set; }
    public IReadOnlyList<string> Permissions { get; private set; } = [];
    public DateTime? AccessTokenExpiresAtUtc { get; private set; }
    public string? AccessToken { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken) &&
                                   AccessTokenExpiresAtUtc > DateTime.UtcNow;
    public bool IsBusy { get; private set; }
    public bool IsRestoreComplete { get; private set; }
    public string? LastError { get; private set; }

    public void BeginRestore()
    {
        IsRestoreComplete = false;
        IsBusy = true;
        LastError = null;
        NotifyChanged();
    }

    public void CompleteRestore()
    {
        IsRestoreComplete = true;
        IsBusy = false;
        NotifyChanged();
    }

    public void SetBusy(bool isBusy)
    {
        IsBusy = isBusy;
        NotifyChanged();
    }

    public void SetError(string? error)
    {
        LastError = error;
        NotifyChanged();
    }

    public void ApplyAuthentication(MobileAuthResponse response)
    {
        AccessToken = response.AccessToken;
        AccessTokenExpiresAtUtc = response.AccessTokenExpiresAtUtc;
        CurrentUser = response.User;
        CurrentBusiness = response.Business;
        BusinessRole = response.Business.Role;
        Permissions = response.Permissions;
        LastError = null;
        NotifyChanged();
    }

    public void ApplyBootstrap(MobileBootstrapResponse response)
    {
        CurrentUser = response.User;
        CurrentBusiness = response.Business;
        BusinessRole = response.BusinessRole;
        Permissions = response.Permissions;
        LastError = null;
        NotifyChanged();
    }

    public void Clear()
    {
        AccessToken = null;
        AccessTokenExpiresAtUtc = null;
        CurrentUser = null;
        CurrentBusiness = null;
        BusinessRole = null;
        Permissions = [];
        IsBusy = false;
        LastError = null;
        NotifyChanged();
    }

    private void NotifyChanged() => Changed?.Invoke();
}
