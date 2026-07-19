using DukkanPilot.Mobile.Core.Api;
using DukkanPilot.Mobile.Core.Contracts;

namespace DukkanPilot.Mobile.Core.State;

public sealed class DashboardState
{
    private readonly IMobileApiClient _apiClient;

    public DashboardState(IMobileApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public event Action? Changed;
    public MobileDashboardTodayResponse? Today { get; private set; }
    public IReadOnlyList<MobileOrderListItem> RecentOrders { get; private set; } = [];
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
            var dashboardTask = _apiClient.GetDashboardTodayAsync(cancellationToken);
            var ordersTask = _apiClient.GetOrdersAsync(1, 5, cancellationToken: cancellationToken);
            await Task.WhenAll(dashboardTask, ordersTask);
            Today = await dashboardTask;
            RecentOrders = (await ordersTask).Items;
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

    public void Clear()
    {
        Today = null;
        RecentOrders = [];
        IsBusy = false;
        LastError = null;
        Changed?.Invoke();
    }
}
