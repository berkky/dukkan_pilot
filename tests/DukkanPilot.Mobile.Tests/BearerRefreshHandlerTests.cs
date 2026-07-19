using System.Net;
using System.Net.Http.Json;
using DukkanPilot.Mobile.Core.Api;
using DukkanPilot.Mobile.Core.Diagnostics;
using DukkanPilot.Mobile.Core.Security;
using DukkanPilot.Mobile.Core.State;

namespace DukkanPilot.Mobile.Tests;

public sealed class BearerRefreshHandlerTests
{
    [Fact]
    public async Task BearerHandlerAddsAccessToken()
    {
        var inner = new RecordingHttpMessageHandler((_, _, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var fixture = await CreateFixtureAsync(inner);

        using var response = await fixture.Client.GetAsync("api/mobile/v1/bootstrap");

        Assert.Equal("Bearer", inner.Requests.Single().AuthorizationScheme);
        Assert.Equal("access-1", inner.Requests.Single().AuthorizationParameter);
    }

    [Fact]
    public async Task UnauthorizedTriggersOneRefreshAndOneRetry()
    {
        var inner = new RecordingHttpMessageHandler((_, count, _) =>
            Task.FromResult(new HttpResponseMessage(
                count == 1 ? HttpStatusCode.Unauthorized : HttpStatusCode.OK)));
        var fixture = await CreateFixtureAsync(inner);

        using var response = await fixture.Client.GetAsync("api/mobile/v1/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, fixture.RefreshApi.RefreshCalls);
        Assert.Equal(2, inner.Requests.Count);
        Assert.Equal("access-2", inner.Requests[1].AuthorizationParameter);
    }

    [Fact]
    public async Task ConcurrentUnauthorizedResponsesUseSingleRefresh()
    {
        var refreshStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var inner = new RecordingHttpMessageHandler((request, _, _) =>
            Task.FromResult(new HttpResponseMessage(
                request.AuthorizationParameter == "access-1"
                    ? HttpStatusCode.Unauthorized
                    : HttpStatusCode.OK)));
        var fixture = await CreateFixtureAsync(inner);
        fixture.RefreshApi.Refresh = async (_, cancellationToken) =>
        {
            refreshStarted.TrySetResult();
            await Task.Delay(75, cancellationToken);
            return TestData.Auth("2");
        };

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => fixture.Client.GetAsync("api/mobile/v1/orders"))
            .ToArray();
        await refreshStarted.Task;
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.Equal(1, fixture.RefreshApi.RefreshCalls);
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task RefreshEndpointDoesNotEnterRefreshLoop()
    {
        var inner = new RecordingHttpMessageHandler((_, _, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var fixture = await CreateFixtureAsync(inner);

        using var response = await fixture.Client.PostAsJsonAsync(
            "api/mobile/v1/auth/refresh",
            new { refreshToken = "not-logged" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, fixture.RefreshApi.RefreshCalls);
        Assert.Single(inner.Requests);
    }

    [Fact]
    public async Task SecondUnauthorizedClearsSession()
    {
        var inner = new RecordingHttpMessageHandler((_, _, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var fixture = await CreateFixtureAsync(inner);

        using var response = await fixture.Client.GetAsync("api/mobile/v1/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(fixture.Session.IsAuthenticated);
        Assert.Null(fixture.Store.Value);
        Assert.Equal(2, inner.Requests.Count);
    }

    [Fact]
    public async Task ApiTimeoutMapsToFriendlyError()
    {
        var inner = new RecordingHttpMessageHandler((_, _, _) =>
            Task.FromException<HttpResponseMessage>(new TaskCanceledException("timeout")));
        using var httpClient = new HttpClient(inner)
        {
            BaseAddress = new Uri("https://api.example.test/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        var api = new MobileApiClient(httpClient);

        var exception = await Assert.ThrowsAsync<MobileApiException>(
            () => api.GetDashboardTodayAsync());

        Assert.Equal("timeout", exception.Code);
        Assert.Equal("Sunucu yanıt vermedi. Lütfen tekrar deneyin.", exception.UserMessage);
    }

    [Fact]
    public async Task ProblemDetailsCodeAndTraceIdAreParsed()
    {
        var inner = new RecordingHttpMessageHandler((_, _, _) =>
            Task.FromResult(RecordingHttpMessageHandler.Problem(
                "invalid_order_status",
                HttpStatusCode.BadRequest,
                "trace-37b")));
        using var httpClient = new HttpClient(inner)
        {
            BaseAddress = new Uri("https://api.example.test/")
        };
        var api = new MobileApiClient(httpClient);

        var exception = await Assert.ThrowsAsync<MobileApiException>(
            () => api.GetOrderAsync(42));

        Assert.Equal("invalid_order_status", exception.Code);
        Assert.Equal("trace-37b", exception.TraceId);
    }

    [Fact]
    public void SafeRequestLogDoesNotContainTokenPasswordOrBody()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.example.test/api/mobile/v1/auth/login")
        {
            Content = JsonContent.Create(new
            {
                email = "owner@example.test",
                password = "super-secret",
                refreshToken = "refresh-secret"
            })
        };
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "access-secret");

        var log = SafeRequestLogFormatter.Format(request);

        Assert.Equal("POST /api/mobile/v1/auth/login", log);
        Assert.DoesNotContain("secret", log, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("owner@example.test", log);
    }

    private static async Task<HandlerFixture> CreateFixtureAsync(HttpMessageHandler inner)
    {
        var session = new SessionState();
        var store = new FakeSecureTokenStore();
        var refreshApi = new StubMobileApiClient();
        var manager = new MobileTokenManager(session, store, refreshApi);
        await manager.ApplyAsync(TestData.Auth());
        var handler = new BearerRefreshHandler(session, manager, inner);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.test/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        return new HandlerFixture(client, session, store, refreshApi);
    }

    private sealed record HandlerFixture(
        HttpClient Client,
        SessionState Session,
        FakeSecureTokenStore Store,
        StubMobileApiClient RefreshApi);
}
