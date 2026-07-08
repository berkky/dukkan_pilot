namespace DukkanPilot.Core.Entities;

public class Notification
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int? BusinessId { get; set; }
    public int? UserId { get; set; }
    public string? TargetRole { get; set; }
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
    public DateTime? ExpiresAtUtc { get; set; }
    public string? MetadataJson { get; set; }
}
