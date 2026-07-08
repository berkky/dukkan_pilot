using DukkanPilot.Core.Entities;

namespace DukkanPilot.Web.Areas.Admin.Models;

public class AdminBillingViewModel
{
    public decimal OpenAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal PaidThisMonth { get; set; }
    public int NewInvoicesThisMonth { get; set; }
    public int OverdueInvoicesCount { get; set; }
    public int ManualPaymentsThisMonth { get; set; }
    public int WonWithoutInvoiceCount { get; set; }

    public string? Search { get; set; }
    public int? BusinessId { get; set; }
    public string? Status { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime? DueFrom { get; set; }
    public DateTime? DueTo { get; set; }

    public List<AdminBillingInvoiceRowViewModel> Invoices { get; set; } = new();
    public List<AdminBusinessOptionViewModel> Businesses { get; set; } = new();
}

public class AdminBillingInvoiceRowViewModel
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}

public class AdminBusinessOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AdminBillingCreateInvoiceViewModel
{
    public int BusinessId { get; set; }
    public int? SubscriptionPlanId { get; set; }
    public int? RelatedSalesRequestId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public decimal? TaxAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime IssueDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime DueDate { get; set; } = DateTime.UtcNow.Date.AddDays(7);
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public string? BusinessVisibleNote { get; set; }
    public string? AdminNotes { get; set; }

    public List<AdminBusinessOptionViewModel> Businesses { get; set; } = new();
}

public class AdminBillingDetailsViewModel
{
    public BillingInvoice Invoice { get; set; } = new();
    public string BusinessName { get; set; } = string.Empty;
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public List<BillingPayment> Payments { get; set; } = new();
    public string DisclaimerText { get; set; } =
        "Bu ekran iç tahsilat takibi içindir. Resmi fatura/e-Belge yerine geçmez; resmi süreçler muhasebe/e-Belge kanalından yürütülmelidir.";
}

public class AdminBillingRecordPaymentViewModel
{
    public int? BillingInvoiceId { get; set; }
    public int BusinessId { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;
    public string Method { get; set; } = "BankTransfer";
    public string Status { get; set; } = "Confirmed";
    public string? ReferenceNumber { get; set; }
    public string? PayerName { get; set; }
    public string? BusinessVisibleNote { get; set; }
    public string? AdminNotes { get; set; }

    public string? InvoiceNumber { get; set; }
    public string? BusinessName { get; set; }
    public decimal? InvoiceTotalAmount { get; set; }
    public string? InvoiceCurrency { get; set; }

    public List<AdminBusinessOptionViewModel> Businesses { get; set; } = new();
}

public class AdminBillingPaymentsViewModel
{
    public int? BusinessId { get; set; }
    public string? Method { get; set; }
    public string? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public List<AdminBusinessOptionViewModel> Businesses { get; set; } = new();
    public List<BillingPayment> Payments { get; set; } = new();
}

