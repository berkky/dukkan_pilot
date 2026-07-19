using DukkanPilot.Mobile.Core.Session;

namespace DukkanPilot.Mobile.Services;

public sealed class AppStartupService
{
    private readonly IMobileSessionService _sessionService;
    private readonly ApiEndpointConfiguration _configuration;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _started;

    public AppStartupService(
        IMobileSessionService sessionService,
        ApiEndpointConfiguration configuration)
    {
        _sessionService = sessionService;
        _configuration = configuration;
    }

    public string? ConfigurationError => _configuration.ConfigurationError;

    public async Task EnsureStartedAsync(CancellationToken cancellationToken = default)
    {
        if (_started || ConfigurationError is not null)
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_started)
            {
                return;
            }

            await _sessionService.RestoreAsync(cancellationToken);
            _started = true;
        }
        finally
        {
            _lock.Release();
        }
    }
}
