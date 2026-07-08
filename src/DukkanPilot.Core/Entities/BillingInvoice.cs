namespace DukkanPilot.Core.Entities;

/// <summary>
/// Internal billing/invoice tracking record. Not an official e-invoice/e-archive document.
/// </summary>
public class BillingInvoice
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public int BusinessId { get; set; }
    public int? SubscriptionPlanId { get; set; }
    public int? BusinessSubscriptionId { get; set; }

    /// <summary>Internal tracking number, not an official invoice number.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public decimal Amount { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = "TRY";

    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }

    public string Status { get; set; } = "Draft";
    public string PaymentStatus { get; set; } = "Unpaid";
    public string Source { get; set; } = "Manual";

    public int? RelatedSalesRequestId { get; set; }

    public string? AdminNotes { get; set; }
    public string? BusinessVisibleNote { get; set; }

    public bool IsOfficialInvoice { get; set; }
    public string? OfficialInvoiceReference { get; set; }

    public int? CreatedByUserId { get; set; }
    public string? CreatedByUserEmail { get; set; }

    public string? MetadataJson { get; set; }
}

