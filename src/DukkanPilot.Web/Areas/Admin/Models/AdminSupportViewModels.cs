using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DukkanPilot.Web.Areas.Admin.Models;

public class AdminSupportListViewModel
{
    public AdminSupportSummaryViewModel Summary { get; set; } = new();
    public int? BusinessId { get; set; }
    public string? Status { get; set; }
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public string? Assigned { get; set; }
    public string? Search { get; set; }
    public List<AdminSupportTicketRowViewModel> Items { get; set; } = [];
    public List<AdminSupportAttentionRowViewModel> AttentionItems { get; set; } = [];
    public List<SelectListItem> BusinessOptions { get; set; } = [];
}

public class AdminSupportSummaryViewModel
{
    public int NewCount { get; set; }
    public int OpenOrInProgressCount { get; set; }
    public int WaitingCustomerCount { get; set; }
    public int UrgentOrHighCount { get; set; }
    public int ResolvedThisMonthCount { get; set; }
    public int WaitingAdminCount { get; set; }
}

public class AdminSupportTicketRowViewModel
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastMessageAtUtc { get; set; }
    public string? LastMessageByRole { get; set; }
    public string? AssignedAdminEmail { get; set; }
}

public class AdminSupportAttentionRowViewModel
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class AdminSupportDetailsViewModel
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessSlug { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? CreatedByUserName { get; set; }
    public string? CreatedByUserEmail { get; set; }
    public string? AssignedAdminEmail { get; set; }
    public string? RelatedEntityName { get; set; }
    public string? ResolutionSummary { get; set; }
    public string? AdminInternalNote { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public bool CanManage { get; set; } = true;
    public List<AdminSupportMessageViewModel> Messages { get; set; } = [];
    public AdminSupportReplyFormViewModel ReplyForm { get; set; } = new();
    public AdminSupportInternalNoteFormViewModel InternalNoteForm { get; set; } = new();
    public AdminSupportStatusFormViewModel StatusForm { get; set; } = new();
    public AdminSupportPriorityFormViewModel PriorityForm { get; set; } = new();
    public AdminSupportAssignFormViewModel AssignForm { get; set; } = new();
    public List<SelectListItem> StatusOptions { get; set; } = [];
    public List<SelectListItem> PriorityOptions { get; set; } = [];
}

public class AdminSupportMessageViewModel
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? SenderName { get; set; }
    public string SenderRole { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public bool IsSystemMessage { get; set; }
}

public class AdminSupportReplyFormViewModel
{
    public int TicketId { get; set; }

    [Required(ErrorMessage = "Yanıt zorunludur.")]
    [StringLength(4000)]
    public string Message { get; set; } = string.Empty;
}

public class AdminSupportInternalNoteFormViewModel
{
    public int TicketId { get; set; }

    [Required(ErrorMessage = "İç not zorunludur.")]
    [StringLength(4000)]
    public string Message { get; set; } = string.Empty;
}

public class AdminSupportStatusFormViewModel
{
    public int TicketId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ResolutionSummary { get; set; }
}

public class AdminSupportPriorityFormViewModel
{
    public int TicketId { get; set; }
    public string Priority { get; set; } = "Normal";
}

public class AdminSupportAssignFormViewModel
{
    public int TicketId { get; set; }
    public string? AssignedAdminEmail { get; set; }
}
