using System.Net;
using System.Net.Http.Headers;
using DukkanPilot.Mobile.Core.State;

namespace DukkanPilot.Mobile.Core.Security;

public sealed class BearerRefreshHandler : DelegatingHandler
{
    private readonly SessionState _session;
    private readonly MobileTokenManager _tokenManager;

    public BearerRefreshHandler(
        SessionState session,
        MobileTokenManager tokenManager,
        HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        _session = session;
        _tokenManager = tokenManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var observedToken = _session.AccessToken;
        AddBearer(request, observedToken);
        var bypassRefresh = IsRefreshBypassed(request.RequestUri);
        using var retry = bypassRefresh
            ? null
            : await CloneAsync(request, cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized || bypassRefresh)
        {
            return response;
        }

        if (!await _tokenManager.TryRefreshAsync(observedToken, cancellationToken))
        {
            return response;
        }

        response.Dispose();
        AddBearer(retry!, _session.AccessToken);
        var retriedResponse = await base.SendAsync(retry!, cancellationToken);
        if (retriedResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _tokenManager.ClearAsync(cancellationToken);
        }

        return retriedResponse;
    }

    private static void AddBearer(HttpRequestMessage request, string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private static bool IsRefreshBypassed(Uri? requestUri)
    {
        var path = requestUri?.AbsolutePath ?? string.Empty;
        return path.EndsWith("/auth/login", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith("/auth/refresh", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith("/auth/logout", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith("/auth/logout-all", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<HttpRequestMessage> CloneAsync(
        HttpRequestMessage source,
        CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(source.Method, source.RequestUri)
        {
            Version = source.Version,
            VersionPolicy = source.VersionPolicy
        };

        foreach (var header in source.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (source.Content is not null)
        {
            var bytes = await source.Content.ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in source.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        foreach (var option in source.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        return clone;
    }
}
