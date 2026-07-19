using DukkanPilot.Mobile.Core.Connectivity;

namespace DukkanPilot.Mobile.Services;

public sealed class MauiConnectivityService : IConnectivityService, IDisposable
{
    public MauiConnectivityService()
    {
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    public bool IsOnline => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    public event Action<bool>? ConnectivityChanged;

    public void Dispose()
    {
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs args)
    {
        ConnectivityChanged?.Invoke(args.NetworkAccess == NetworkAccess.Internet);
    }
}
