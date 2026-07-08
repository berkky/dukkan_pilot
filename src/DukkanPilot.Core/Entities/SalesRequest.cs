namespace DukkanPilot.Core.Entities;

public class SalesRequest
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public int? BusinessId { get; set; }
    public int? RequestedPlanId { get; set; }
    public int? CurrentPlanId { get; set; }

    public string Source { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string Status { get; set; } = "New";
    public string Priority { get; set; } = "Normal";

    public string? ContactName { get; set; }
    public string? BusinessName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? RequestedPlanName { get; set; }
    public string? CurrentPlanName { get; set; }
    public string? Message { get; set; }
    public string? AdminNotes { get; set; }

    public DateTime? LastContactedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public string? ClosedReason { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? MetadataJson { get; set; }

    public bool PrivacyNoticeAcknowledged { get; set; }
    public bool KvkkNoticeAcknowledged { get; set; }
}
