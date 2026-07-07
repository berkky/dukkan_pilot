namespace DukkanPilot.Web.Helpers;

using DukkanPilot.Web.Areas.Business.Models;

public static class CustomerCrmHelper
{
    public const decimal VipSpendThreshold = 1000m;
    public const int VipOrderThreshold = 5;
    public const int ReturningOrderThreshold = 2;
    public const int RiskDaysThreshold = 30;
    public const int NewDaysThreshold = 30;

    public static string DeterminePrimarySegment(CustomerCrmStats stats, DateTime utcNow)
    {
        if (!stats.IsActive)
        {
            return CustomerSegment.Passive;
        }

        if (stats.OrderCount == 0)
        {
            return CustomerSegment.NoOrders;
        }

        if (stats.TotalSpent >= VipSpendThreshold || stats.OrderCount >= VipOrderThreshold)
        {
            return CustomerSegment.Vip;
        }

        if (IsAtRisk(stats, utcNow))
        {
            return CustomerSegment.AtRisk;
        }

        if (IsNewCustomer(stats, utcNow))
        {
            return CustomerSegment.New;
        }

        if (stats.OrderCount >= ReturningOrderThreshold)
        {
            return CustomerSegment.Returning;
        }

        return CustomerSegment.Standard;
    }

    public static bool IsVip(CustomerCrmStats stats) =>
        stats.TotalSpent >= VipSpendThreshold || stats.OrderCount >= VipOrderThreshold;

    public static bool IsReturning(CustomerCrmStats stats) =>
        stats.OrderCount >= ReturningOrderThreshold;

    public static bool IsNewCustomer(CustomerCrmStats stats, DateTime utcNow)
    {
        var threshold = utcNow.AddDays(-NewDaysThreshold);
        if (stats.FirstOrderDate.HasValue && stats.FirstOrderDate.Value >= threshold)
        {
            return true;
        }

        return stats.CreatedAt >= threshold;
    }

    public static bool IsAtRisk(CustomerCrmStats stats, DateTime utcNow)
    {
        if (stats.OrderCount < 1 || !stats.LastOrderDate.HasValue)
        {
            return false;
        }

        return (utcNow - stats.LastOrderDate.Value).TotalDays > RiskDaysThreshold;
    }

    public static bool MatchesSegmentFilter(string segmentFilter, CustomerCrmStats stats, DateTime utcNow)
    {
        var normalized = string.IsNullOrWhiteSpace(segmentFilter)
            ? "all"
            : segmentFilter.Trim().ToLowerInvariant();

        return normalized switch
        {
            "vip" => IsVip(stats),
            "returning" => IsReturning(stats),
            "new" => IsNewCustomer(stats, utcNow),
            "atrisk" => IsAtRisk(stats, utcNow),
            "passive" => !stats.IsActive,
            "noorders" => stats.OrderCount == 0,
            _ => true
        };
    }

    public static bool MatchesLastOrderFilter(string? lastOrderFilter, CustomerCrmStats stats, DateTime utcNow)
    {
        var normalized = string.IsNullOrWhiteSpace(lastOrderFilter)
            ? "all"
            : lastOrderFilter.Trim().ToLowerInvariant();

        if (normalized == "all")
        {
            return true;
        }

        if (normalized == "none")
        {
            return !stats.LastOrderDate.HasValue;
        }

        if (!stats.LastOrderDate.HasValue)
        {
            return false;
        }

        var lastOrder = stats.LastOrderDate.Value;
        var todayStart = utcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        return normalized switch
        {
            "today" => lastOrder >= todayStart && lastOrder < todayEnd,
            "week" => lastOrder >= utcNow.AddDays(-7),
            "month" => lastOrder >= utcNow.AddDays(-30),
            "older30" => lastOrder < utcNow.AddDays(-30),
            _ => true
        };
    }

    public static string GetSegmentLabel(string segment) => segment switch
    {
        CustomerSegment.Vip => "VIP",
        CustomerSegment.Returning => "Tekrar Gelen",
        CustomerSegment.New => "Yeni",
        CustomerSegment.AtRisk => "Riskte",
        CustomerSegment.Passive => "Pasif",
        CustomerSegment.NoOrders => "Sipariş Yok",
        _ => "Standart"
    };

    public static string GetSegmentBadgeClass(string segment) => segment switch
    {
        CustomerSegment.Vip => "bg-warning text-dark",
        CustomerSegment.Returning => "bg-primary",
        CustomerSegment.New => "bg-info text-dark",
        CustomerSegment.AtRisk => "bg-danger",
        CustomerSegment.Passive => "bg-secondary",
        CustomerSegment.NoOrders => "bg-light text-dark border",
        _ => "bg-secondary"
    };

    public static string? BuildWhatsAppContactUrl(string? phone)
        => OrderDisplayHelper.BuildWhatsAppContactUrl(phone);
}

public static class CustomerSegment
{
    public const string Vip = "vip";
    public const string Returning = "returning";
    public const string New = "new";
    public const string AtRisk = "atrisk";
    public const string Passive = "passive";
    public const string NoOrders = "noorders";
    public const string Standard = "standard";
}

public sealed class CustomerCrmStats
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public int TotalPoints { get; init; }

    public bool IsActive { get; init; }

    public DateTime CreatedAt { get; init; }

    public int OrderCount { get; set; }

    public decimal TotalSpent { get; set; }

    public decimal AverageBasket { get; set; }

    public DateTime? LastOrderDate { get; set; }

    public DateTime? FirstOrderDate { get; set; }

    public decimal MaxOrderAmount { get; set; }

    public int Last30DaysOrderCount { get; set; }

    public string Segment { get; set; } = CustomerSegment.Standard;

    public string? WhatsAppContactUrl { get; set; }
}
