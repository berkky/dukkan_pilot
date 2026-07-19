namespace DukkanPilot.Mobile.Core.Connectivity;

public interface IConnectivityService
{
    bool IsOnline { get; }
    event Action<bool>? ConnectivityChanged;
}
