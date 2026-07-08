namespace DukkanPilot.Core.Entities;

public class SupportTicketMessage
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public int SupportTicketId { get; set; }
    public int BusinessId { get; set; }

    public int? SenderUserId { get; set; }
    public string? SenderEmail { get; set; }
    public string? SenderName { get; set; }
    public string SenderRole { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public bool IsSystemMessage { get; set; }
    public string? MetadataJson { get; set; }

    public SupportTicket? SupportTicket { get; set; }
}
