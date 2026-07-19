using DukkanPilot.Mobile.Core.Api;
using DukkanPilot.Mobile.Core.Connectivity;
using DukkanPilot.Mobile.Core.Contracts;

namespace DukkanPilot.Mobile.Core.State;

public sealed class OrderState
{
    private readonly IMobileApiClient _apiClient;
    private readonly IConnectivityService _connectivity;
    private readonly List<MobileOrderListItem> _items = [];

    public OrderState(IMobileApiClient apiClient, IConnectivityService connectivity)
    {
        _apiClient = apiClient;
        _connectivity = connectivity;
    }

    public event Action? Changed;
    public IReadOnlyList<MobileOrderListItem> Items => _items;
    public MobileOrderDetails? SelectedOrder { get; private set; }
    public int Page { get; private set; }
    public int PageSize { get; } = 20;
    public int TotalCount { get; private set; }
    public string? StatusFilter { get; private set; }
    public bool HasMore => _items.Count < TotalCount;
    public bool IsBusy { get; private set; }
    public string? LastError { get; private set; }
    public string? TraceId { get; private set; }

    public Task LoadFirstPageAsync(
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        StatusFilter = string.IsNullOrWhiteSpace(status) ? null : status;
        return LoadPageAsync(1, reset: true, cancellationToken);
    }

    public Task LoadMoreAsync(CancellationToken cancellationToken = default)
    {
        return HasMore
            ? LoadPageAsync(Page + 1, reset: false, cancellationToken)
            : Task.CompletedTask;
    }

    public async Task LoadDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        SetBusy();
        try
        {
            SelectedOrder = await _apiClient.GetOrderAsync(id, cancellationToken);
            LastError = null;
            TraceId = null;
        }
        catch (MobileApiException exception)
        {
            if (exception.Code == "resource_not_found")
            {
                SelectedOrder = null;
            }

            LastError = exception.UserMessage;
            TraceId = exception.TraceId;
            throw;
        }
        finally
        {
            IsBusy = false;
            Changed?.Invoke();
        }
    }

    public async Task<bool> UpdateStatusAsync(
        int id,
        string targetStatus,
        CancellationToken cancellationToken = default)
    {
        if (!_connectivity.IsOnline)
        {
            LastError = MobileErrorMessages.ForCode("offline");
            Changed?.Invoke();
            return false;
        }

        var originalDetails = SelectedOrder;
        SetBusy();
        try
        {
            var updated = await _apiClient.UpdateOrderStatusAsync(
                id,
                targetStatus,
                cancellationToken);
            SelectedOrder = updated;
            var index = _items.FindIndex(order => order.Id == id);
            if (index >= 0)
            {
                var old = _items[index];
                _items[index] = old with
                {
                    Status = updated.Status,
                    TotalAmount = updated.TotalAmount
                };
            }

            LastError = null;
            TraceId = null;
            return true;
        }
        catch (MobileApiException exception)
        {
            SelectedOrder = originalDetails;
            LastError = exception.UserMessage;
            TraceId = exception.TraceId;
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
        _items.Clear();
        SelectedOrder = null;
        Page = 0;
        TotalCount = 0;
        StatusFilter = null;
        IsBusy = false;
        LastError = null;
        TraceId = null;
        Changed?.Invoke();
    }

    private async Task LoadPageAsync(
        int page,
        bool reset,
        CancellationToken cancellationToken)
    {
        SetBusy();
        try
        {
            var response = await _apiClient.GetOrdersAsync(
                page,
                PageSize,
                StatusFilter,
                cancellationToken);
            if (reset)
            {
                _items.Clear();
            }

            var knownIds = _items.Select(order => order.Id).ToHashSet();
            _items.AddRange(response.Items.Where(order => knownIds.Add(order.Id)));
            Page = response.Page;
            TotalCount = response.TotalCount;
            LastError = null;
            TraceId = null;
        }
        catch (MobileApiException exception)
        {
            LastError = exception.UserMessage;
            TraceId = exception.TraceId;
            throw;
        }
        finally
        {
            IsBusy = false;
            Changed?.Invoke();
        }
    }

    private void SetBusy()
    {
        IsBusy = true;
        LastError = null;
        TraceId = null;
        Changed?.Invoke();
    }
}
