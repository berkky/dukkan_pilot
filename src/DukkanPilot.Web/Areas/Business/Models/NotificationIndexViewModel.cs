namespace DukkanPilot.Web.Areas.Business.Models;

public class NotificationIndexViewModel
{
    public List<NotificationRowViewModel> Items { get; set; } = new();
    public string? Search { get; set; }
    public string? Type { get; set; }
    public string? Severity { get; set; }
    public bool UnreadOnly { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public int UnreadCount { get; set; }
    public int TodayCount { get; set; }
    public int CriticalCount { get; set; }
    public int WarningCount { get; set; }
}

public class NotificationRowViewModel
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string Area { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string? EntityName { get; set; }
    public int? EntityId { get; set; }
    public string Severity { get; set; } = "Info";
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public int? BusinessId { get; set; }
    public string? BusinessName { get; set; }
}

public class AdminNotificationIndexViewModel : NotificationIndexViewModel
{
    public int? BusinessId { get; set; }
    public int BusinessAlertCount { get; set; }
}

public class NotificationSummaryViewModel
{
    public int UnreadCount { get; set; }
    public int CriticalCount { get; set; }
    public string? LatestTitle { get; set; }
    public string? LatestUrl { get; set; }
}

public class DashboardNotificationCardViewModel
{
    public int UnreadCount { get; set; }
    public int CriticalCount { get; set; }
    public List<NotificationRowViewModel> RecentItems { get; set; } = new();
}
