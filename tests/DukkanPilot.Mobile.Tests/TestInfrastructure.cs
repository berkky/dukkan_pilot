using System.Net;
using System.Text;
using System.Text.Json;
using DukkanPilot.Mobile.Core.Api;
using DukkanPilot.Mobile.Core.Connectivity;
using DukkanPilot.Mobile.Core.Contracts;
using DukkanPilot.Mobile.Core.Security;

namespace DukkanPilot.Mobile.Tests;

internal static class TestData
{
    public static MobileAuthResponse Auth(string suffix = "1") =>
        new(
            $"access-{suffix}",
            $"refresh-{suffix}",
            DateTime.UtcNow.AddMinutes(15),
            DateTime.UtcNow.AddDays(30),
            new MobileUserSummary(7, "Test Kullanıcı", "owner@example.test", "BusinessOwner"),
            new MobileBusinessSummary(11, "Merkez", "Owner"),
            ["orders.read", "orders.status.update", "kitchen.read", "dashboard.read"]);

    public static MobileBootstrapResponse Bootstrap() =>
        new(
            Auth().User,
            Auth().Business,
            "Owner",
            Auth().Permissions,
            new MobilePlanSummary("Pro", "Active", DateTime.UtcNow.AddMonths(1), true),
            ["dashboard", "orders", "kitchen"],
            DateTime.UtcNow);

    public static MobileOrderListItem ListOrder(int id, string status = "Pending") =>
        new(id, $"DP-{id:000}", 120m + id, status, "Web", "DineIn", "Masa 1", "Müşteri", DateTime.UtcNow);

    public static MobileOrderDetails Details(int id = 1, string status = "Pending") =>
        new(
            id,
            $"DP-{id:000}",
            120m,
            0m,
            120m,
            status,
            "Web",
            "DineIn",
            "Masa 1",
            "Müşteri",
            null,
            "Az tuzlu",
            DateTime.UtcNow,
            [new MobileOrderItem("Çorba", 2, 60m, 120m)]);
}

internal sealed class FakeSecureTokenStore : ISecureTokenStore
{
    public SecureTokenRecord? Value { get; set; }
    public int SaveCount { get; private set; }
    public int ClearCount { get; private set; }
    public bool ThrowOnRead { get; set; }

    public Task<SecureTokenRecord?> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowOnRead)
        {
            throw new InvalidOperationException("Corrupt secure storage");
        }

        return Task.FromResult(Value);
    }

    public Task SaveAsync(SecureTokenRecord token, CancellationToken cancellationToken = default)
    {
        Value = token;
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        Value = null;
        ClearCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeConnectivityService : IConnectivityService
{
    private bool _isOnline = true;
    public bool IsOnline => _isOnline;
    public event Action<bool>? ConnectivityChanged;

    public void SetOnline(bool online)
    {
        _isOnline = online;
        ConnectivityChanged?.Invoke(online);
    }
}

internal sealed class StubMobileApiClient : IMobileApiClient
{
    public Func<MobileLoginRequest, CancellationToken, Task<MobileAuthResponse>>? Login { get; set; }
    public Func<string, CancellationToken, Task<MobileAuthResponse>>? Refresh { get; set; }
    public Func<CancellationToken, Task<MobileBootstrapResponse>>? Bootstrap { get; set; }
    public Func<int, int, string?, CancellationToken, Task<PagedResponse<MobileOrderListItem>>>? Orders { get; set; }
    public Func<int, CancellationToken, Task<MobileOrderDetails>>? Order { get; set; }
    public Func<int, string, CancellationToken, Task<MobileOrderDetails>>? UpdateStatus { get; set; }
    public Func<CancellationToken, Task<MobileKitchenResponse>>? Kitchen { get; set; }
    public Func<CancellationToken, Task<MobileDashboardTodayResponse>>? Dashboard { get; set; }
    public MobileLoginRequest? LastLoginRequest { get; private set; }
    public int LoginCalls { get; private set; }
    public int RefreshCalls { get; private set; }
    public int LogoutCalls { get; private set; }
    public int LogoutAllCalls { get; private set; }
    public int UpdateStatusCalls { get; private set; }
    public int KitchenCalls { get; private set; }

    public Task<MobileAuthResponse> LoginAsync(MobileLoginRequest request, CancellationToken cancellationToken = default)
    {
        LoginCalls++;
        LastLoginRequest = request;
        return Login?.Invoke(request, cancellationToken) ?? Task.FromResult(TestData.Auth());
    }

    public Task<MobileAuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        RefreshCalls++;
        return Refresh?.Invoke(refreshToken, cancellationToken) ?? Task.FromResult(TestData.Auth("2"));
    }

    public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        LogoutCalls++;
        return Task.CompletedTask;
    }

    public Task LogoutAllAsync(CancellationToken cancellationToken = default)
    {
        LogoutAllCalls++;
        return Task.CompletedTask;
    }

    public Task<MobileMeResponse> GetMeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new MobileMeResponse(TestData.Auth().User, TestData.Auth().Business, TestData.Auth().Permissions));

    public Task<MobileBootstrapResponse> GetBootstrapAsync(CancellationToken cancellationToken = default) =>
        Bootstrap?.Invoke(cancellationToken) ?? Task.FromResult(TestData.Bootstrap());

    public Task<PagedResponse<MobileOrderListItem>> GetOrdersAsync(
        int page,
        int pageSize,
        string? status = null,
        CancellationToken cancellationToken = default) =>
        Orders?.Invoke(page, pageSize, status, cancellationToken) ??
        Task.FromResult(new PagedResponse<MobileOrderListItem>(page, pageSize, 0, []));

    public Task<MobileOrderDetails> GetOrderAsync(int id, CancellationToken cancellationToken = default) =>
        Order?.Invoke(id, cancellationToken) ?? Task.FromResult(TestData.Details(id));

    public Task<MobileOrderDetails> UpdateOrderStatusAsync(
        int id,
        string status,
        CancellationToken cancellationToken = default)
    {
        UpdateStatusCalls++;
        return UpdateStatus?.Invoke(id, status, cancellationToken) ??
               Task.FromResult(TestData.Details(id, status));
    }

    public Task<MobileKitchenResponse> GetKitchenOrdersAsync(CancellationToken cancellationToken = default)
    {
        KitchenCalls++;
        return Kitchen?.Invoke(cancellationToken) ??
               Task.FromResult(new MobileKitchenResponse(0, 0, [], DateTime.UtcNow));
    }

    public Task<MobileDashboardTodayResponse> GetDashboardTodayAsync(CancellationToken cancellationToken = default) =>
        Dashboard?.Invoke(cancellationToken) ??
        Task.FromResult(new MobileDashboardTodayResponse(0, 0, 0, 0, 0, 0m, DateTime.UtcNow));
}

internal sealed record RequestSnapshot(
    HttpMethod Method,
    string PathAndQuery,
    string? AuthorizationScheme,
    string? AuthorizationParameter,
    string? Body);

internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<RequestSnapshot, int, CancellationToken, Task<HttpResponseMessage>> _response;
    private int _count;

    public RecordingHttpMessageHandler(
        Func<RequestSnapshot, int, CancellationToken, Task<HttpResponseMessage>> response)
    {
        _response = response;
    }

    public List<RequestSnapshot> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);
        var snapshot = new RequestSnapshot(
            request.Method,
            request.RequestUri?.PathAndQuery ?? string.Empty,
            request.Headers.Authorization?.Scheme,
            request.Headers.Authorization?.Parameter,
            body);
        lock (Requests)
        {
            Requests.Add(snapshot);
        }
        return await _response(snapshot, Interlocked.Increment(ref _count), cancellationToken);
    }

    public static HttpResponseMessage Json<T>(T value, HttpStatusCode status = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(value, MobileApiClient.JsonOptions),
                Encoding.UTF8,
                "application/json")
        };
    }

    public static HttpResponseMessage Problem(
        string code,
        HttpStatusCode status,
        string? traceId = "trace-test")
    {
        return Json(new
        {
            type = $"https://httpstatuses.com/{(int)status}",
            title = "Problem",
            status = (int)status,
            code,
            traceId
        }, status);
    }
}
