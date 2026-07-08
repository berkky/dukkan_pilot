using System.Globalization;

namespace DukkanPilot.Web.Helpers;

public static class BillingDisplayHelper
{
    public static string GetInvoiceStatusLabel(string? status) => (status ?? "").Trim() switch
    {
        "Draft" => "Taslak",
        "Issued" => "Kesildi",
        "PartiallyPaid" => "Kısmi ödendi",
        "Paid" => "Ödendi",
        "Overdue" => "Gecikmiş",
        "Cancelled" => "İptal",
        _ => string.IsNullOrWhiteSpace(status) ? "-" : status
    };

    public static string GetInvoiceStatusBadgeClass(string? status) => (status ?? "").Trim() switch
    {
        "Draft" => "bg-light text-dark border",
        "Issued" => "bg-primary",
        "PartiallyPaid" => "bg-warning text-dark",
        "Paid" => "bg-success",
        "Overdue" => "bg-danger",
        "Cancelled" => "bg-secondary",
        _ => "bg-secondary"
    };

    public static string GetPaymentStatusLabel(string? status) => (status ?? "").Trim() switch
    {
        "Unpaid" => "Ödenmedi",
        "Partial" => "Kısmi",
        "Paid" => "Ödendi",
        "Refunded" => "İade",
        "Cancelled" => "İptal",
        _ => string.IsNullOrWhiteSpace(status) ? "-" : status
    };

    public static string GetPaymentStatusBadgeClass(string? status) => (status ?? "").Trim() switch
    {
        "Unpaid" => "bg-light text-dark border",
        "Partial" => "bg-warning text-dark",
        "Paid" => "bg-success",
        "Refunded" => "bg-secondary",
        "Cancelled" => "bg-secondary",
        _ => "bg-secondary"
    };

    public static string GetPaymentMethodLabel(string? method) => (method ?? "").Trim() switch
    {
        "BankTransfer" => "Havale/EFT",
        "Cash" => "Nakit",
        "Manual" => "Manuel",
        "Other" => "Diğer",
        _ => string.IsNullOrWhiteSpace(method) ? "-" : method
    };

    public static string GetDueStatusText(DateTime dueDateUtc)
    {
        var today = DateTime.UtcNow.Date;
        var due = dueDateUtc.Date;
        var diff = (due - today).Days;

        if (diff < 0) return $"{Math.Abs(diff)} gün gecikti";
        if (diff == 0) return "Bugün vadesi";
        if (diff == 1) return "Yarın vadesi";
        return $"{diff} gün kaldı";
    }

    public static string FormatMoney(decimal amount, string currency)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var formatted = amount.ToString("N2", culture);
        return string.Equals(currency, "TRY", StringComparison.OrdinalIgnoreCase)
            ? $"{formatted} ₺"
            : $"{formatted} {currency}";
    }
}

