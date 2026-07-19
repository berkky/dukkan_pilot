using System.Net.Http.Json;
using System.Text.Json;
using DukkanPilot.Mobile.Core.Contracts;

namespace DukkanPilot.Mobile.Core.Api;

public sealed class MobileApiClient : IMobileApiClient
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private const string Root = "api/mobile/v1/";
    private readonly HttpClient _httpClient;

    public MobileApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<MobileAuthResponse> LoginAsync(
        MobileLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<MobileAuthResponse>(
            new HttpRequestMessage(HttpMethod.Post, $"{Root}auth/login")
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            },
            cancellationToken);
    }

    public Task<MobileAuthResponse> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<MobileAuthResponse>(
            new HttpRequestMessage(HttpMethod.Post, $"{Root}auth/refresh")
            {
                Content = JsonContent.Create(
                    new MobileRefreshRequest { RefreshToken = refreshToken },
                    options: JsonOptions)
            },
            cancellationToken);
    }

    public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return SendNoContentAsync(
            new HttpRequestMessage(HttpMethod.Post, $"{Root}auth/logout")
            {
                Content = JsonContent.Create(
                    new MobileLogoutRequest { RefreshToken = refreshToken },
                    options: JsonOptions)
            },
            cancellationToken);
    }

    public Task LogoutAllAsync(CancellationToken cancellationToken = default)
    {
        return SendNoContentAsync(
            new HttpRequestMessage(HttpMethod.Post, $"{Root}auth/logout-all"),
            cancellationToken);
    }

    public Task<MobileMeResponse> GetMeAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync<MobileMeResponse>(
            new HttpRequestMessage(HttpMethod.Get, $"{Root}auth/me"),
            cancellationToken);
    }

    public Task<MobileBootstrapResponse> GetBootstrapAsync(
        CancellationToken cancellationToken = default)
    {
        return SendAsync<MobileBootstrapResponse>(
            new HttpRequestMessage(HttpMethod.Get, $"{Root}bootstrap"),
            cancellationToken);
    }

    public Task<PagedResponse<MobileOrderListItem>> GetOrdersAsync(
        int page,
        int pageSize,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = $"{Root}orders?page={Math.Max(1, page)}&pageSize={Math.Clamp(pageSize, 1, 100)}";
        if (!string.IsNullOrWhiteSpace(status))
        {
            query += $"&status={Uri.EscapeDataString(status.Trim())}";
        }

        return SendAsync<PagedResponse<MobileOrderListItem>>(
            new HttpRequestMessage(HttpMethod.Get, query),
            cancellationToken);
    }

    public Task<MobileOrderDetails> GetOrderAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<MobileOrderDetails>(
            new HttpRequestMessage(HttpMethod.Get, $"{Root}orders/{id}"),
            cancellationToken);
    }

    public Task<MobileOrderDetails> UpdateOrderStatusAsync(
        int id,
        string status,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<MobileOrderDetails>(
            new HttpRequestMessage(HttpMethod.Post, $"{Root}orders/{id}/status")
            {
                Content = JsonContent.Create(
                    new MobileOrderStatusRequest { Status = status },
                    options: JsonOptions)
            },
            cancellationToken);
    }

    public Task<MobileKitchenResponse> GetKitchenOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        return SendAsync<MobileKitchenResponse>(
            new HttpRequestMessage(HttpMethod.Get, $"{Root}kitchen/orders"),
            cancellationToken);
    }

    public Task<MobileDashboardTodayResponse> GetDashboardTodayAsync(
        CancellationToken cancellationToken = default)
    {
        return SendAsync<MobileDashboardTodayResponse>(
            new HttpRequestMessage(HttpMethod.Get, $"{Root}dashboard/today"),
            cancellationToken);
    }

    private async Task<T> SendAsync<T>(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using (request)
        using (var response = await SendCoreAsync(request, cancellationToken))
        {
            if (!response.IsSuccessStatusCode)
            {
                throw await ProblemDetailsParser.ParseAsync(response, cancellationToken);
            }

            var result = await response.Content.ReadFromJsonAsync<T>(
                JsonOptions,
                cancellationToken);
            return result ?? throw new MobileApiException(
                "invalid_response",
                "Sunucudan geçerli bir yanıt alınamadı.",
                response.StatusCode);
        }
    }

    private async Task SendNoContentAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using (request)
        using (var response = await SendCoreAsync(request, cancellationToken))
        {
            if (!response.IsSuccessStatusCode)
            {
                throw await ProblemDetailsParser.ParseAsync(response, cancellationToken);
            }
        }
    }

    private async Task<HttpResponseMessage> SendCoreAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MobileApiException(
                "timeout",
                MobileErrorMessages.ForCode("timeout"),
                innerException: exception);
        }
        catch (HttpRequestException exception)
        {
            throw new MobileApiException(
                "connection_error",
                MobileErrorMessages.ForCode("connection_error"),
                innerException: exception);
        }
    }
}
