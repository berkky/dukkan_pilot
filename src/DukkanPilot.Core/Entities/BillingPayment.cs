namespace DukkanPilot.Core.Entities;

/// <summary>
/// Internal manual payment tracking record. No card/bank-account sensitive data stored.
/// </summary>
public class BillingPayment
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public int BusinessId { get; set; }
    public int? BillingInvoiceId { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime PaymentDate { get; set; }

    public string Method { get; set; } = "BankTransfer";
    public string Status { get; set; } = "Confirmed";

    public string? ReferenceNumber { get; set; }
    public string? PayerName { get; set; }

    public string? AdminNotes { get; set; }
    public string? BusinessVisibleNote { get; set; }

    public int? RecordedByUserId { get; set; }
    public string? RecordedByUserEmail { get; set; }

    public string? MetadataJson { get; set; }
}

