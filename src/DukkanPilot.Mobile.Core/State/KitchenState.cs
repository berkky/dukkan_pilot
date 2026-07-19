using DukkanPilot.Mobile.Core.Api;
using DukkanPilot.Mobile.Core.Connectivity;
using DukkanPilot.Mobile.Core.Contracts;

namespace DukkanPilot.Mobile.Core.State;

public sealed class KitchenState
{
    private readonly IMobileApiClient _apiClient;
    private readonly IConnectivityService _connectivity;

    public KitchenState(IMobileApiClient apiClient, IConnectivityService connectivity)
    {
        _apiClient = apiClient;
        _connectivity = connectivity;
    }

    public event Action? Changed;
    public MobileKitchenResponse? Data { get; private set; }
    public bool IsBusy { get; private set; }
    public string? LastError { get; private set; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        LastError = null;
        Changed?.Invoke();
        try
        {
            Data = await _apiClient.GetKitchenOrdersAsync(cancellationToken);
        }
        catch (MobileApiException exception)
        {
            LastError = exception.UserMessage;
        }
        finally
        {
            IsBusy = false;
            Changed?.Invoke();
        }
    }

    public async Task<bool> UpdateStatusAsync(
        int orderId,
        string targetStatus,
        CancellationToken cancellationToken = default)
    {
        if (!_connectivity.IsOnline)
        {
            LastError = MobileErrorMessages.ForCode("offline");
            Changed?.Invoke();
            return false;
        }

        IsBusy = true;
        Changed?.Invoke();
        try
        {
            await _apiClient.UpdateOrderStatusAsync(orderId, targetStatus, cancellationToken);
            Data = await _apiClient.GetKitchenOrdersAsync(cancellationToken);
            LastError = null;
            return true;
        }
        catch (MobileApiException exception)
        {
            LastError = exception.UserMessage;
            return false;
        }
        finally
        {
            IsBusy = false;
            Changed?.Invoke();
        }
    }

    public void Clear()
    {
        Data = null;
        IsBusy = false;
        LastError = null;
        Changed?.Invoke();
    }
}
