namespace DukkanPilot.Core.Entities;

public class SupportTicket
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }

    public int BusinessId { get; set; }
    public int? CreatedByUserId { get; set; }
    public string? CreatedByUserEmail { get; set; }
    public string? CreatedByUserName { get; set; }
    public int? AssignedAdminUserId { get; set; }
    public string? AssignedAdminEmail { get; set; }

    public string TicketNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "New";
    public string Source { get; set; } = "BusinessPanel";

    public string? RelatedEntityName { get; set; }
    public int? RelatedEntityId { get; set; }

    public DateTime? LastMessageAtUtc { get; set; }
    public string? LastMessageByRole { get; set; }

    public int? CustomerSatisfactionScore { get; set; }
    public string? ResolutionSummary { get; set; }
    public string? AdminInternalNote { get; set; }
    public string? MetadataJson { get; set; }

    public ICollection<SupportTicketMessage> Messages { get; set; } = new List<SupportTicketMessage>();
}
