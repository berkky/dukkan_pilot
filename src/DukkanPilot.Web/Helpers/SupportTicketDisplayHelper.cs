namespace DukkanPilot.Web.Helpers;

public static class SupportTicketDisplayHelper
{
    public static readonly string[] AllowedCategories =
    [
        "Technical", "Order", "Menu", "Billing", "Account", "Campaign", "Loyalty",
        "Report", "Onboarding", "FeatureRequest", "Other"
    ];

    public static readonly string[] AllowedPriorities = ["Low", "Normal", "High", "Urgent"];

    public static readonly string[] AllowedStatuses =
    [
        "New", "Open", "InProgress", "WaitingCustomer", "WaitingAdmin",
        "Resolved", "Closed", "Cancelled"
    ];

    public static readonly string[] AllowedSources = ["BusinessPanel", "AdminCreated", "Feedback"];

    public static readonly string[] RelatedScreens =
    [
        "Menu", "Orders", "Campaigns", "Billing", "Login", "Public QR", "Other"
    ];

    public static readonly string[] OpenStatuses =
    [
        "New", "Open", "InProgress", "WaitingCustomer", "WaitingAdmin"
    ];

    public static readonly string[] ClosedStatuses = ["Resolved", "Closed", "Cancelled"];

    public static readonly string[] HighPriorityStatuses = ["High", "Urgent"];

    public static string GetStatusLabel(string? status) => status switch
    {
        "New" => "Yeni",
        "Open" => "Açık",
        "InProgress" => "İşleniyor",
        "WaitingCustomer" => "Müşteri yanıtı bekleniyor",
        "WaitingAdmin" => "Destek yanıtı bekleniyor",
        "Resolved" => "Çözüldü",
        "Closed" => "Kapatıldı",
        "Cancelled" => "İptal",
        _ => status ?? "-"
    };

    public static string GetStatusBadgeClass(string? status) => status switch
    {
        "New" => "bg-primary",
        "Open" => "bg-info text-dark",
        "InProgress" => "bg-warning text-dark",
        "WaitingCustomer" => "bg-secondary",
        "WaitingAdmin" => "bg-warning text-dark",
        "Resolved" => "bg-success",
        "Closed" => "bg-dark",
        "Cancelled" => "bg-light text-dark",
        _ => "bg-light text-dark"
    };

    public static string GetPriorityLabel(string? priority) => priority switch
    {
        "Low" => "Düşük",
        "Normal" => "Normal",
        "High" => "Yüksek",
        "Urgent" => "Acil",
        _ => priority ?? "-"
    };

    public static string GetPriorityBadgeClass(string? priority) => priority switch
    {
        "Urgent" => "bg-danger",
        "High" => "bg-warning text-dark",
        "Low" => "bg-secondary",
        _ => "bg-light text-dark"
    };

    public static string GetCategoryLabel(string? category) => category switch
    {
        "Technical" => "Teknik",
        "Order" => "Sipariş",
        "Menu" => "Menü / QR",
        "Billing" => "Abonelik / Tahsilat",
        "Account" => "Hesap / Yetki",
        "Campaign" => "Kampanya",
        "Loyalty" => "Sadakat",
        "Report" => "Rapor",
        "Onboarding" => "Kurulum",
        "FeatureRequest" => "Özellik isteği",
        "Other" => "Diğer",
        _ => category ?? "-"
    };

    public static string GetSourceLabel(string? source) => source switch
    {
        "BusinessPanel" => "İşletme paneli",
        "AdminCreated" => "Admin oluşturdu",
        "Feedback" => "Geri bildirim",
        _ => source ?? "-"
    };

    public static string GetRelatedScreenLabel(string? name) => name switch
    {
        "Menu" => "Menü / Ürünler",
        "Orders" => "Siparişler",
        "Campaigns" => "Kampanyalar",
        "Billing" => "Abonelik / Tahsilat",
        "Login" => "Giriş / Hesap",
        "Public QR" => "Public QR menü",
        "Other" => "Diğer",
        _ => name ?? "-"
    };

    public static string GetAgeText(DateTime createdAtUtc, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var span = now - createdAtUtc;
        if (span.TotalMinutes < 60)
        {
            return $"{Math.Max(1, (int)span.TotalMinutes)} dk";
        }

        if (span.TotalHours < 48)
        {
            return $"{(int)span.TotalHours} sa";
        }

        return $"{(int)span.TotalDays} gün";
    }

    public static string GetLastMessageText(DateTime? lastMessageAtUtc, string? lastMessageByRole)
    {
        if (lastMessageAtUtc is null)
        {
            return "Henüz mesaj yok";
        }

        var role = lastMessageByRole switch
        {
            "Admin" => "Destek",
            "Business" => "İşletme",
            "System" => "Sistem",
            _ => "—"
        };

        return $"{lastMessageAtUtc.Value.ToLocalTime():dd.MM.yyyy HH:mm} · {role}";
    }

    public static bool IsOpenStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && OpenStatuses.Contains(status, StringComparer.Ordinal);

    public static bool IsClosedStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && ClosedStatuses.Contains(status, StringComparer.Ordinal);

    public static bool IsAllowedStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && AllowedStatuses.Contains(status, StringComparer.Ordinal);

    public static bool IsAllowedCategory(string? category) =>
        !string.IsNullOrWhiteSpace(category) && AllowedCategories.Contains(category, StringComparer.Ordinal);

    public static bool IsAllowedPriority(string? priority) =>
        !string.IsNullOrWhiteSpace(priority) && AllowedPriorities.Contains(priority, StringComparer.Ordinal);

    public static bool ShouldNotifyBusinessOnStatusChange(string? newStatus) =>
        newStatus is "WaitingCustomer" or "Resolved" or "Closed";
}
