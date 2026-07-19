namespace DukkanPilot.Mobile.Core.Contracts;

public sealed class MobileLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int? BusinessId { get; set; }
}

public sealed class MobileRefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class MobileLogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed record MobileUserSummary(int Id, string FullName, string Email, string Role);
public sealed record MobileBusinessSummary(int Id, string Name, string Role);
public sealed record MobileBusinessOption(int Id, string Name, string Role);

public sealed record MobileAuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc,
    MobileUserSummary User,
    MobileBusinessSummary Business,
    IReadOnlyList<string> Permissions);

public sealed record MobileMeResponse(
    MobileUserSummary User,
    MobileBusinessSummary Business,
    IReadOnlyList<string> Permissions);

public sealed record MobilePlanSummary(
    string Name,
    string Status,
    DateTime? EndsAtUtc,
    bool HasValidSubscription);

public sealed record MobileBootstrapResponse(
    MobileUserSummary User,
    MobileBusinessSummary Business,
    string BusinessRole,
    IReadOnlyList<string> Permissions,
    MobilePlanSummary Subscription,
    IReadOnlyList<string> AvailableModules,
    DateTime ServerTimeUtc);

public sealed record PagedResponse<T>(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<T> Items);

public sealed record MobileOrderListItem(
    int Id,
    string OrderNumber,
    decimal TotalAmount,
    string Status,
    string Source,
    string? ServiceType,
    string? TableLabel,
    string? CustomerName,
    DateTime CreatedAtUtc);

public sealed record MobileOrderItem(
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record MobileOrderDetails(
    int Id,
    string OrderNumber,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    string Status,
    string Source,
    string? ServiceType,
    string? TableLabel,
    string? CustomerName,
    string? CustomerPhone,
    string? Notes,
    DateTime CreatedAtUtc,
    IReadOnlyList<MobileOrderItem> Items);

public sealed class MobileOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public sealed record MobileKitchenResponse(
    int PendingCount,
    int PreparingCount,
    IReadOnlyList<MobileOrderDetails> Orders,
    DateTime ServerTimeUtc);

public sealed record MobileDashboardTodayResponse(
    int TotalOrders,
    int PendingOrders,
    int PreparingOrders,
    int CompletedOrders,
    int CancelledOrders,
    decimal Revenue,
    DateTime ServerTimeUtc);

public static class MobileOrderStatuses
{
    public const string Pending = "Pending";
    public const string Preparing = "Preparing";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All =
        [Pending, Preparing, Completed, Cancelled];

    public static IReadOnlyList<string> GetAllowedTargets(string current)
    {
        return current switch
        {
            Pending => [Preparing, Cancelled],
            Preparing => [Completed, Cancelled],
            _ => []
        };
    }

    public static string ToTurkish(string status)
    {
        return status switch
        {
            Pending => "Bekliyor",
            Preparing => "Hazırlanıyor",
            Completed => "Tamamlandı",
            Cancelled => "İptal",
            _ => status
        };
    }
}
