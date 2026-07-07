namespace DukkanPilot.Web.Areas.Business.Models;

public class PlanUsageMetricViewModel
{
    public string Name { get; set; } = string.Empty;

    public int Used { get; set; }

    public int Limit { get; set; }

    public bool IsUnlimited => Limit < 0;

    public bool IsLimitReached => !IsUnlimited && Used >= Limit;

    public string LimitDisplay => IsUnlimited ? "Limitsiz" : Limit.ToString();

    public string UsageDisplay => IsUnlimited ? $"{Used} / Limitsiz" : $"{Used} / {Limit}";

    public int UsagePercent
    {
        get
        {
            if (IsUnlimited || Limit <= 0)
            {
                return IsLimitReached ? 100 : 0;
            }

            return (int)Math.Min(100, Math.Round(Used * 100.0 / Limit));
        }
    }

    public bool IsNearLimit => !IsUnlimited && !IsLimitReached && UsagePercent >= 80;

    public string StatusText => IsLimitReached ? "Limit doldu" : "Kullanılabilir";

    public string StatusCssClass => IsLimitReached ? "text-danger" : IsNearLimit ? "text-warning" : "text-success";

    public string ProgressBarClass => IsLimitReached ? "bg-danger" : IsNearLimit ? "bg-warning" : "bg-success";
}
