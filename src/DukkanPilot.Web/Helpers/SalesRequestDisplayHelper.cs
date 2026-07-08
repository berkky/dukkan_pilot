namespace DukkanPilot.Web.Helpers;

public static class SalesRequestDisplayHelper
{
    public static readonly string[] AllowedStatuses =
    [
        "New", "Contacted", "Qualified", "WaitingCustomer", "Won", "Lost", "Cancelled"
    ];

    public static readonly string[] AllowedPriorities = ["Low", "Normal", "High"];

    public static readonly string[] OpenStatuses = ["New", "Contacted", "Qualified", "WaitingCustomer"];

    public static string GetStatusLabel(string? status) => status switch
    {
        "New" => "Yeni",
        "Contacted" => "İletişime geçildi",
        "Qualified" => "Nitelikli",
        "WaitingCustomer" => "Müşteri bekleniyor",
        "Won" => "Kazanıldı",
        "Lost" => "Kaybedildi",
        "Cancelled" => "İptal",
        _ => status ?? "-"
    };

    public static string GetStatusBadgeClass(string? status) => status switch
    {
        "New" => "bg-primary",
        "Contacted" => "bg-info text-dark",
        "Qualified" => "bg-success",
        "WaitingCustomer" => "bg-warning text-dark",
        "Won" => "bg-success",
        "Lost" => "bg-secondary",
        "Cancelled" => "bg-dark",
        _ => "bg-light text-dark"
    };

    public static string GetPriorityBadgeClass(string? priority) => priority switch
    {
        "High" => "bg-danger",
        "Low" => "bg-secondary",
        _ => "bg-light text-dark"
    };

    public static string GetPriorityLabel(string? priority) => priority switch
    {
        "High" => "Yüksek",
        "Low" => "Düşük",
        "Normal" => "Normal",
        _ => priority ?? "-"
    };

    public static string GetSourceLabel(string? source) => source switch
    {
        "PublicPricing" => "Public Pricing",
        "PublicDemo" => "Public Demo",
        "PublicContact" => "Public Contact",
        "BusinessBilling" => "Business Billing",
        "AdminCreated" => "Admin",
        _ => source ?? "-"
    };

    public static string GetRequestTypeLabel(string? type) => type switch
    {
        "DemoRequest" => "Demo talebi",
        "PlanRequest" => "Plan talebi",
        "UpgradeRequest" => "Yükseltme talebi",
        "ContactRequest" => "İletişim",
        "TrialRequest" => "Trial",
        _ => type ?? "-"
    };

    public static bool IsClosedStatus(string? status) =>
        status is "Won" or "Lost" or "Cancelled";

    public static bool IsAllowedStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && AllowedStatuses.Contains(status, StringComparer.Ordinal);
}
