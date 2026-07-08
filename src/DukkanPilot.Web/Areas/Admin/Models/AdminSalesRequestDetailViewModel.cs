using System.ComponentModel.DataAnnotations;

namespace DukkanPilot.Web.Areas.Admin.Models;

public class AdminSalesRequestDetailViewModel
{
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public int? BusinessId { get; set; }

    public string? BusinessName { get; set; }

    public string Source { get; set; } = string.Empty;

    public string RequestType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public string? ContactName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? CurrentPlanName { get; set; }

    public int? CurrentPlanId { get; set; }

    public string? RequestedPlanName { get; set; }

    public int? RequestedPlanId { get; set; }

    public string? Message { get; set; }

    public string? AdminNotes { get; set; }

    public DateTime? LastContactedAtUtc { get; set; }

    public DateTime? ClosedAtUtc { get; set; }

    public string? ClosedReason { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public bool PrivacyNoticeAcknowledged { get; set; }

    public bool KvkkNoticeAcknowledged { get; set; }

    public int? OnboardingScore { get; set; }

    public string? OnboardingStatusLabel { get; set; }

    public string? OnboardingBadgeClass { get; set; }

    public string? OnboardingNextAction { get; set; }

    public int? CustomerSuccessScore { get; set; }

    public string? CustomerSuccessStatusLabel { get; set; }

    public string? CustomerSuccessBadgeClass { get; set; }

    public string? CustomerSuccessTopRisk { get; set; }

    public AdminSalesRequestUpdateViewModel Update { get; set; } = new();
}

public class AdminSalesRequestUpdateViewModel
{
    [Required]
    [StringLength(40)]
    public string Status { get; set; } = "New";

    [Required]
    [StringLength(40)]
    public string Priority { get; set; } = "Normal";

    [StringLength(2000)]
    public string? AdminNotes { get; set; }

    [StringLength(500)]
    public string? ClosedReason { get; set; }

    public bool MarkContactedNow { get; set; }
}
