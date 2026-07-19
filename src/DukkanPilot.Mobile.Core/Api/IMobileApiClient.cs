using DukkanPilot.Mobile.Core.Contracts;

namespace DukkanPilot.Mobile.Core.Api;

public interface IMobileApiClient
{
    Task<MobileAuthResponse> LoginAsync(MobileLoginRequest request, CancellationToken cancellationToken = default);
    Task<MobileAuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAllAsync(CancellationToken cancellationToken = default);
    Task<MobileMeResponse> GetMeAsync(CancellationToken cancellationToken = default);
    Task<MobileBootstrapResponse> GetBootstrapAsync(CancellationToken cancellationToken = default);
    Task<PagedResponse<MobileOrderListItem>> GetOrdersAsync(
        int page,
        int pageSize,
        string? status = null,
        CancellationToken cancellationToken = default);
    Task<MobileOrderDetails> GetOrderAsync(int id, CancellationToken cancellationToken = default);
    Task<MobileOrderDetails> UpdateOrderStatusAsync(
        int id,
        string status,
        CancellationToken cancellationToken = default);
    Task<MobileKitchenResponse> GetKitchenOrdersAsync(CancellationToken cancellationToken = default);
    Task<MobileDashboardTodayResponse> GetDashboardTodayAsync(CancellationToken cancellationToken = default);
}
