namespace DukkanPilot.Core.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int? BusinessId { get; set; }
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserRole { get; set; }
    public string Area { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public int? EntityId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? MetadataJson { get; set; }
    public string Severity { get; set; } = "Info";
}
