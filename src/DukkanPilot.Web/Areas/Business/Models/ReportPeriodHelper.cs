namespace DukkanPilot.Web.Areas.Business.Models;

public static class ReportPeriodHelper
{
    public static ReportPeriodRange Resolve(string? period, DateTime? startDate, DateTime? endDate)
    {
        var normalizedPeriod = string.IsNullOrWhiteSpace(period)
            ? "last7"
            : period.Trim().ToLowerInvariant();

        var localToday = DateTime.Now.Date;

        return normalizedPeriod switch
        {
            "today" => CreateRange("today", "Bugün", localToday, localToday, false),
            "thismonth" => CreateMonthRange(localToday),
            "custom" when startDate.HasValue && endDate.HasValue => CreateCustomRange(startDate.Value.Date, endDate.Value.Date),
            "last7" => CreateRange("last7", "Son 7 Gün", localToday.AddDays(-6), localToday, false),
            _ when startDate.HasValue && endDate.HasValue => CreateCustomRange(startDate.Value.Date, endDate.Value.Date),
            _ => CreateRange("last7", "Son 7 Gün", localToday.AddDays(-6), localToday, false)
        };
    }

    private static ReportPeriodRange CreateMonthRange(DateTime localToday)
    {
        var monthStart = new DateTime(localToday.Year, localToday.Month, 1);
        return CreateRange("thismonth", "Bu Ay", monthStart, localToday, false);
    }

    private static ReportPeriodRange CreateCustomRange(DateTime startLocal, DateTime endLocal)
    {
        var wasAdjusted = false;
        if (startLocal > endLocal)
        {
            (startLocal, endLocal) = (endLocal, startLocal);
            wasAdjusted = true;
        }

        var label = $"{startLocal:dd.MM.yyyy} – {endLocal:dd.MM.yyyy}";
        return CreateRange("custom", label, startLocal, endLocal, wasAdjusted);
    }

    private static ReportPeriodRange CreateRange(
        string period,
        string periodLabel,
        DateTime startLocal,
        DateTime endLocal,
        bool wasAdjusted)
    {
        return new ReportPeriodRange
        {
            Period = period,
            PeriodLabel = periodLabel,
            StartLocal = startLocal,
            EndLocal = endLocal,
            StartUtc = startLocal.ToUniversalTime(),
            EndUtc = endLocal.AddDays(1).ToUniversalTime(),
            WasDateRangeAdjusted = wasAdjusted
        };
    }
}

public sealed class ReportPeriodRange
{
    public string Period { get; init; } = "last7";

    public string PeriodLabel { get; init; } = "Son 7 Gün";

    public DateTime StartLocal { get; init; }

    public DateTime EndLocal { get; init; }

    public DateTime StartUtc { get; init; }

    public DateTime EndUtc { get; init; }

    public bool WasDateRangeAdjusted { get; init; }
}
