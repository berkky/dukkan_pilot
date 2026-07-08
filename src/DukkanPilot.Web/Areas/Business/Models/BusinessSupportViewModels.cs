using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DukkanPilot.Web.Areas.Business.Models;

public class BusinessSupportListViewModel
{
    public BusinessSupportSummaryViewModel Summary { get; set; } = new();
    public string? Status { get; set; }
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public string? Search { get; set; }
    public List<BusinessSupportTicketRowViewModel> Items { get; set; } = [];
}

public class BusinessSupportSummaryViewModel
{
    public int OpenCount { get; set; }
    public int WaitingCustomerCount { get; set; }
    public int ResolvedOrClosedCount { get; set; }
    public int UrgentOrHighCount { get; set; }
}

public class BusinessSupportTicketRowViewModel
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastMessageAtUtc { get; set; }
    public string? LastMessageByRole { get; set; }
}

public class BusinessSupportCreateViewModel
{
    [Required(ErrorMessage = "Kategori seçin.")]
    public string Category { get; set; } = "Other";

    public string Priority { get; set; } = "Normal";

    [Required(ErrorMessage = "Konu zorunludur.")]
    [StringLength(300)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mesaj zorunludur.")]
    [StringLength(4000)]
    public string Message { get; set; } = string.Empty;

    public string? RelatedEntityName { get; set; }

    public List<SelectListItem> CategoryOptions { get; set; } = [];
    public List<SelectListItem> PriorityOptions { get; set; } = [];
    public List<SelectListItem> RelatedScreenOptions { get; set; } = [];
    public bool IsFeatureRequest { get; set; }
}

public class BusinessSupportDetailsViewModel
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? RelatedEntityName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public string? ResolutionSummary { get; set; }
    public bool CanReply { get; set; }
    public List<BusinessSupportMessageViewModel> Messages { get; set; } = [];
    public BusinessSupportReplyFormViewModel ReplyForm { get; set; } = new();
}

public class BusinessSupportMessageViewModel
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? SenderName { get; set; }
    public string SenderRole { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsSystemMessage { get; set; }
}

public class BusinessSupportReplyFormViewModel
{
    public int TicketId { get; set; }

    [Required(ErrorMessage = "Mesaj zorunludur.")]
    [StringLength(4000)]
    public string Message { get; set; } = string.Empty;
}

public class DashboardSupportCardViewModel
{
    public int OpenCount { get; set; }
    public int UrgentOrHighCount { get; set; }
    public string? LatestTicketNumber { get; set; }
    public string? LatestTicketStatus { get; set; }
    public int? LatestTicketId { get; set; }
}
