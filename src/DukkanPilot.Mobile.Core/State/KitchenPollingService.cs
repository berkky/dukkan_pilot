namespace DukkanPilot.Mobile.Core.State;

public sealed class KitchenPollingService : IAsyncDisposable
{
    private readonly KitchenState _state;
    private readonly TimeSpan _interval;
    private CancellationTokenSource? _cancellation;
    private Task? _pollingTask;

    public KitchenPollingService(KitchenState state, TimeSpan? interval = null)
    {
        _state = state;
        _interval = interval ?? TimeSpan.FromSeconds(20);
    }

    public bool IsRunning => _pollingTask is { IsCompleted: false };

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _cancellation = new CancellationTokenSource();
        _pollingTask = PollAsync(_cancellation.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_cancellation is null)
        {
            return;
        }

        await _cancellation.CancelAsync();
        if (_pollingTask is not null)
        {
            try
            {
                await _pollingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when the kitchen page is closed.
            }
        }

        _cancellation.Dispose();
        _cancellation = null;
        _pollingTask = null;
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        await _state.LoadAsync(cancellationToken);
        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await _state.LoadAsync(cancellationToken);
        }
    }
}
