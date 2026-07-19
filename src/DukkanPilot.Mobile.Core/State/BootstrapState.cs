using DukkanPilot.Mobile.Core.Contracts;

namespace DukkanPilot.Mobile.Core.State;

public sealed class BootstrapState
{
    public event Action? Changed;

    public MobilePlanSummary? Subscription { get; private set; }
    public IReadOnlyList<string> AvailableModules { get; private set; } = [];
    public DateTime? ServerTimeUtc { get; private set; }
    public bool IsBusy { get; private set; }
    public string? LastError { get; private set; }

    public void Apply(MobileBootstrapResponse response)
    {
        Subscription = response.Subscription;
        AvailableModules = response.AvailableModules;
        ServerTimeUtc = response.ServerTimeUtc;
        IsBusy = false;
        LastError = null;
        Changed?.Invoke();
    }

    public void SetBusy(bool busy)
    {
        IsBusy = busy;
        Changed?.Invoke();
    }

    public void SetError(string? error)
    {
        IsBusy = false;
        LastError = error;
        Changed?.Invoke();
    }

    public void Clear()
    {
        Subscription = null;
        AvailableModules = [];
        ServerTimeUtc = null;
        IsBusy = false;
        LastError = null;
        Changed?.Invoke();
    }
}
