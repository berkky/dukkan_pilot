namespace DukkanPilot.Web.Areas.Business.Models;

public static class OrderQueryHelper
{
    public static (DateTime StartUtc, DateTime EndUtc) GetTodayUtcRange()
    {
        var localToday = DateTime.Now.Date;
        return (localToday.ToUniversalTime(), localToday.AddDays(1).ToUniversalTime());
    }

    public static DateTime GetWeekStartUtc()
    {
        return DateTime.Now.Date.AddDays(-6).ToUniversalTime();
    }

    public static (DateTime StartUtc, DateTime EndUtc) GetCurrentMonthUtcRange()
    {
        var localMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        return (localMonthStart.ToUniversalTime(), localMonthStart.AddMonths(1).ToUniversalTime());
    }
}
