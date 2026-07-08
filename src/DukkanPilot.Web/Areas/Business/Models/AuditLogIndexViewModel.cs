namespace DukkanPilot.Web.Areas.Business.Models;

public class AuditLogIndexViewModel
{
    public List<AuditLogRowViewModel> Items { get; set; } = new();
    public string? Search { get; set; }
    public string? Action { get; set; }
    public string? Severity { get; set; }
    public string? UserEmail { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public int TodayCount { get; set; }
    public int Last7DaysCount { get; set; }
    public int CriticalCount { get; set; }
    public DateTime? LatestActivityUtc { get; set; }
}

public class AuditLogRowViewModel
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? UserEmail { get; set; }
    public string? UserRole { get; set; }
    public string Area { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public int? EntityId { get; set; }
    public string Severity { get; set; } = "Info";
    public string? MetadataJson { get; set; }
    public int? BusinessId { get; set; }
    public string? BusinessName { get; set; }
}

public class AdminAuditLogIndexViewModel : AuditLogIndexViewModel
{
    public int? BusinessId { get; set; }
    public string? Area { get; set; }
    public int AdminActionCount { get; set; }
}
