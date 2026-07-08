using System.Globalization;
using DukkanPilot.Core.Entities;
using DukkanPilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Services;

public sealed record BillingInvoiceCreateInput
{
    public int BusinessId { get; init; }
    public int? SubscriptionPlanId { get; init; }
    public int? BusinessSubscriptionId { get; init; }
    public int? RelatedSalesRequestId { get; init; }

    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Amount { get; init; }
    public decimal? TaxAmount { get; init; }
    public string Currency { get; init; } = "TRY";
    public DateTime IssueDate { get; init; }
    public DateTime DueDate { get; init; }
    public DateTime? PeriodStart { get; init; }
    public DateTime? PeriodEnd { get; init; }
    public string? BusinessVisibleNote { get; init; }
    public string? AdminNotes { get; init; }

    public int? CreatedByUserId { get; init; }
    public string? CreatedByUserEmail { get; init; }
    public string Source { get; init; } = "AdminCreated";
}

public sealed record BillingPaymentCreateInput
{
    public int BusinessId { get; init; }
    public int? BillingInvoiceId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "TRY";
    public DateTime PaymentDate { get; init; }
    public string Method { get; init; } = "BankTransfer";
    public string Status { get; init; } = "Confirmed";
    public string? ReferenceNumber { get; init; }
    public string? PayerName { get; init; }
    public string? BusinessVisibleNote { get; init; }
    public string? AdminNotes { get; init; }
    public int? RecordedByUserId { get; init; }
    public string? RecordedByUserEmail { get; init; }
}

public sealed record AdminBillingSummary
{
    public decimal OpenAmount { get; init; }
    public decimal OverdueAmount { get; init; }
    public int OverdueCount { get; init; }
    public decimal PaidThisMonth { get; init; }
    public int NewInvoicesThisMonth { get; init; }
    public int ManualPaymentsThisMonth { get; init; }
    public int WonWithoutInvoiceCount { get; init; }
}

public sealed record BusinessBillingSummary
{
    public decimal OpenAmount { get; init; }
    public decimal OverdueAmount { get; init; }
    public decimal PaidThisMonth { get; init; }
    public DateTime? NextDueDateUtc { get; init; }
    public int OpenInvoiceCount { get; init; }
    public int OverdueInvoiceCount { get; init; }
}

public interface IBillingOperationsService
{
    Task<BillingInvoice> CreateInvoiceAsync(BillingInvoiceCreateInput input, CancellationToken cancellationToken = default);
    Task<BillingPayment> RecordPaymentAsync(BillingPaymentCreateInput input, CancellationToken cancellationToken = default);
    Task RecalculateInvoicePaymentStatusAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task CancelInvoiceAsync(int invoiceId, int? adminUserId, string? adminEmail, CancellationToken cancellationToken = default);

    Task<BusinessBillingSummary> GetBusinessBillingSummaryAsync(int businessId, CancellationToken cancellationToken = default);
    Task<AdminBillingSummary> GetAdminBillingSummaryAsync(CancellationToken cancellationToken = default);

    Task<List<BillingInvoice>> GetOverdueInvoicesAsync(int take = 50, CancellationToken cancellationToken = default);
    Task<List<BillingPayment>> GetRecentPaymentsAsync(int take = 50, CancellationToken cancellationToken = default);
    Task<string> GenerateInvoiceNumberAsync(int businessId, CancellationToken cancellationToken = default);
}

public sealed class BillingOperationsService : IBillingOperationsService
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IAuditLogService _audit;

    public BillingOperationsService(AppDbContext context, INotificationService notifications, IAuditLogService audit)
    {
        _context = context;
        _notifications = notifications;
        _audit = audit;
    }

    public async Task<BillingInvoice> CreateInvoiceAsync(BillingInvoiceCreateInput input, CancellationToken cancellationToken = default)
    {
        var businessExists = await _context.Businesses
            .AsNoTracking()
            .AnyAsync(b => b.Id == input.BusinessId && b.IsActive, cancellationToken);

        if (!businessExists)
        {
            throw new InvalidOperationException("İşletme bulunamadı veya aktif değil.");
        }

        var invoiceNumber = await GenerateInvoiceNumberAsync(input.BusinessId, cancellationToken);

        var amount = Math.Max(0m, input.Amount);
        var tax = input.TaxAmount.HasValue ? Math.Max(0m, input.TaxAmount.Value) : (decimal?)null;
        var total = amount + (tax ?? 0m);

        var invoice = new BillingInvoice
        {
            CreatedAtUtc = DateTime.UtcNow,
            BusinessId = input.BusinessId,
            SubscriptionPlanId = input.SubscriptionPlanId,
            BusinessSubscriptionId = input.BusinessSubscriptionId,
            RelatedSalesRequestId = input.RelatedSalesRequestId,
            InvoiceNumber = invoiceNumber,
            Title = (input.Title ?? "").Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            Amount = amount,
            TaxAmount = tax,
            TotalAmount = total,
            Currency = string.IsNullOrWhiteSpace(input.Currency) ? "TRY" : input.Currency.Trim().ToUpperInvariant(),
            IssueDate = input.IssueDate,
            DueDate = input.DueDate,
            PeriodStart = input.PeriodStart,
            PeriodEnd = input.PeriodEnd,
            Status = "Issued",
            PaymentStatus = "Unpaid",
            Source = string.IsNullOrWhiteSpace(input.Source) ? "AdminCreated" : input.Source.Trim(),
            AdminNotes = string.IsNullOrWhiteSpace(input.AdminNotes) ? null : input.AdminNotes.Trim(),
            BusinessVisibleNote = string.IsNullOrWhiteSpace(input.BusinessVisibleNote) ? null : input.BusinessVisibleNote.Trim(),
            IsOfficialInvoice = false,
            CreatedByUserId = input.CreatedByUserId,
            CreatedByUserEmail = string.IsNullOrWhiteSpace(input.CreatedByUserEmail) ? null : input.CreatedByUserEmail.Trim()
        };

        _context.BillingInvoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.LogAdminAsync(
            action: "Billing.InvoiceCreated",
            entityName: nameof(BillingInvoice),
            entityId: invoice.Id,
            summary: $"Yeni tahsilat kaydı oluşturuldu: {invoice.InvoiceNumber} ({invoice.TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))} {invoice.Currency})",
            metadata: new
            {
                invoice.InvoiceNumber,
                invoice.TotalAmount,
                invoice.Currency,
                invoice.DueDate,
                invoice.RelatedSalesRequestId
            },
            businessId: invoice.BusinessId,
            cancellationToken: cancellationToken);

        await _notifications.CreateBusinessAsync(
            businessId: invoice.BusinessId,
            type: "BillingInvoiceCreated",
            title: "Yeni tahsilat kaydı oluşturuldu",
            message: "Ödeme/tahsilat kaydı oluşturuldu. Bu kayıt resmi fatura/e-Belge yerine geçmez.",
            actionUrl: "/Business/Billing/Invoices",
            severity: "Info",
            entityName: nameof(BillingInvoice),
            entityId: invoice.Id,
            metadata: new { invoice.InvoiceNumber, invoice.DueDate },
            cancellationToken: cancellationToken);

        return invoice;
    }

    public async Task<BillingPayment> RecordPaymentAsync(BillingPaymentCreateInput input, CancellationToken cancellationToken = default)
    {
        var businessExists = await _context.Businesses
            .AsNoTracking()
            .AnyAsync(b => b.Id == input.BusinessId && b.IsActive, cancellationToken);

        if (!businessExists)
        {
            throw new InvalidOperationException("İşletme bulunamadı veya aktif değil.");
        }

        BillingInvoice? invoice = null;
        if (input.BillingInvoiceId.HasValue)
        {
            invoice = await _context.BillingInvoices
                .FirstOrDefaultAsync(i => i.Id == input.BillingInvoiceId.Value && i.BusinessId == input.BusinessId, cancellationToken);

            if (invoice is null)
            {
                throw new InvalidOperationException("Fatura/tahsilat kaydı bulunamadı.");
            }
        }

        var payment = new BillingPayment
        {
            CreatedAtUtc = DateTime.UtcNow,
            BusinessId = input.BusinessId,
            BillingInvoiceId = input.BillingInvoiceId,
            Amount = Math.Max(0m, input.Amount),
            Currency = string.IsNullOrWhiteSpace(input.Currency) ? "TRY" : input.Currency.Trim().ToUpperInvariant(),
            PaymentDate = input.PaymentDate,
            Method = string.IsNullOrWhiteSpace(input.Method) ? "BankTransfer" : input.Method.Trim(),
            Status = string.IsNullOrWhiteSpace(input.Status) ? "Confirmed" : input.Status.Trim(),
            ReferenceNumber = string.IsNullOrWhiteSpace(input.ReferenceNumber) ? null : input.ReferenceNumber.Trim(),
            PayerName = string.IsNullOrWhiteSpace(input.PayerName) ? null : input.PayerName.Trim(),
            BusinessVisibleNote = string.IsNullOrWhiteSpace(input.BusinessVisibleNote) ? null : input.BusinessVisibleNote.Trim(),
            AdminNotes = string.IsNullOrWhiteSpace(input.AdminNotes) ? null : input.AdminNotes.Trim(),
            RecordedByUserId = input.RecordedByUserId,
            RecordedByUserEmail = string.IsNullOrWhiteSpace(input.RecordedByUserEmail) ? null : input.RecordedByUserEmail.Trim()
        };

        _context.BillingPayments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.LogAdminAsync(
            action: "Billing.PaymentRecorded",
            entityName: nameof(BillingPayment),
            entityId: payment.Id,
            summary: $"Ödeme kaydı işlendi ({payment.Amount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))} {payment.Currency})",
            metadata: new { payment.BillingInvoiceId, payment.Amount, payment.Currency, payment.Method, payment.Status, payment.PaymentDate },
            businessId: payment.BusinessId,
            cancellationToken: cancellationToken);

        await _notifications.CreateBusinessAsync(
            businessId: payment.BusinessId,
            type: "BillingPaymentRecorded",
            title: "Ödeme kaydınız işlendi",
            message: "Ödeme kaydı işlendi. Bu kayıt resmi fatura/e-Belge yerine geçmez.",
            actionUrl: "/Business/Billing/Payments",
            severity: "Success",
            entityName: nameof(BillingPayment),
            entityId: payment.Id,
            metadata: new { payment.Amount, payment.PaymentDate },
            cancellationToken: cancellationToken);

        if (invoice is not null)
        {
            await RecalculateInvoicePaymentStatusAsync(invoice.Id, cancellationToken);
        }

        return payment;
    }

    public async Task CancelInvoiceAsync(int invoiceId, int? adminUserId, string? adminEmail, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.BillingInvoices.FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);
        if (invoice is null)
        {
            return;
        }

        invoice.UpdatedAtUtc = DateTime.UtcNow;
        invoice.Status = "Cancelled";
        invoice.PaymentStatus = "Cancelled";
        _context.BillingInvoices.Update(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.LogAdminAsync(
            action: "Billing.InvoiceCancelled",
            entityName: nameof(BillingInvoice),
            entityId: invoice.Id,
            summary: $"Tahsilat kaydı iptal edildi: {invoice.InvoiceNumber}",
            metadata: new { invoice.InvoiceNumber },
            businessId: invoice.BusinessId,
            cancellationToken: cancellationToken);

        await _notifications.CreateBusinessAsync(
            businessId: invoice.BusinessId,
            type: "BillingInvoiceCancelled",
            title: "Tahsilat kaydı iptal edildi",
            message: "Bir tahsilat kaydı iptal edildi. Bu kayıt resmi fatura/e-Belge yerine geçmez.",
            actionUrl: "/Business/Billing/Invoices",
            severity: "Warning",
            entityName: nameof(BillingInvoice),
            entityId: invoice.Id,
            metadata: new { invoice.InvoiceNumber },
            cancellationToken: cancellationToken);
    }

    public async Task RecalculateInvoicePaymentStatusAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.BillingInvoices.FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);
        if (invoice is null)
        {
            return;
        }

        if (invoice.Status == "Cancelled")
        {
            return;
        }

        var paidAmount = await _context.BillingPayments
            .AsNoTracking()
            .Where(p => p.BillingInvoiceId == invoice.Id && p.Status == "Confirmed")
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var total = Math.Max(0m, invoice.TotalAmount);
        var now = DateTime.UtcNow;

        invoice.PaymentStatus = paidAmount <= 0m
            ? "Unpaid"
            : paidAmount + 0.0001m < total
                ? "Partial"
                : "Paid";

        invoice.Status = invoice.PaymentStatus switch
        {
            "Paid" => "Paid",
            "Partial" => now.Date > invoice.DueDate.Date ? "Overdue" : "PartiallyPaid",
            _ => now.Date > invoice.DueDate.Date ? "Overdue" : "Issued"
        };

        invoice.UpdatedAtUtc = now;
        _context.BillingInvoices.Update(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        if (invoice.PaymentStatus == "Paid")
        {
            await _notifications.CreateBusinessAsync(
                businessId: invoice.BusinessId,
                type: "BillingInvoicePaid",
                title: "Ödeme tamamlandı",
                message: "Tahsilat kaydı ödendi olarak güncellendi. Abonelik süresi otomatik uzatılmaz; gerekirse admin tarafından ayrıca güncellenir.",
                actionUrl: "/Business/Billing/Invoices",
                severity: "Success",
                entityName: nameof(BillingInvoice),
                entityId: invoice.Id,
                metadata: new { invoice.InvoiceNumber },
                cancellationToken: cancellationToken);
        }
    }

    public async Task<BusinessBillingSummary> GetBusinessBillingSummaryAsync(int businessId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var invoices = _context.BillingInvoices.AsNoTracking().Where(i => i.BusinessId == businessId && i.Status != "Cancelled");
        var open = await invoices
            .Where(i => i.PaymentStatus != "Paid" && i.PaymentStatus != "Cancelled")
            .SumAsync(i => (decimal?)i.TotalAmount, cancellationToken) ?? 0m;

        var overdue = await invoices
            .Where(i => i.Status == "Overdue")
            .SumAsync(i => (decimal?)i.TotalAmount, cancellationToken) ?? 0m;

        var overdueCount = await invoices.CountAsync(i => i.Status == "Overdue", cancellationToken);
        var openCount = await invoices.CountAsync(i => i.PaymentStatus != "Paid" && i.PaymentStatus != "Cancelled", cancellationToken);

        var paidThisMonth = await _context.BillingPayments
            .AsNoTracking()
            .Where(p => p.BusinessId == businessId && p.Status == "Confirmed" && p.PaymentDate >= monthStart)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var nextDue = await invoices
            .Where(i => i.PaymentStatus != "Paid" && i.PaymentStatus != "Cancelled")
            .OrderBy(i => i.DueDate)
            .Select(i => (DateTime?)i.DueDate)
            .FirstOrDefaultAsync(cancellationToken);

        return new BusinessBillingSummary
        {
            OpenAmount = open,
            OverdueAmount = overdue,
            PaidThisMonth = paidThisMonth,
            NextDueDateUtc = nextDue,
            OpenInvoiceCount = openCount,
            OverdueInvoiceCount = overdueCount
        };
    }

    public async Task<AdminBillingSummary> GetAdminBillingSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var invoices = _context.BillingInvoices.AsNoTracking().Where(i => i.Status != "Cancelled");

        var open = await invoices
            .Where(i => i.PaymentStatus != "Paid" && i.PaymentStatus != "Cancelled")
            .SumAsync(i => (decimal?)i.TotalAmount, cancellationToken) ?? 0m;

        var overdueAmount = await invoices
            .Where(i => i.Status == "Overdue")
            .SumAsync(i => (decimal?)i.TotalAmount, cancellationToken) ?? 0m;

        var overdueCount = await invoices.CountAsync(i => i.Status == "Overdue", cancellationToken);

        var newInvoicesThisMonth = await invoices.CountAsync(i => i.CreatedAtUtc >= monthStart, cancellationToken);

        var paidThisMonth = await _context.BillingPayments
            .AsNoTracking()
            .Where(p => p.Status == "Confirmed" && p.PaymentDate >= monthStart)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var manualPaymentsThisMonth = await _context.BillingPayments
            .AsNoTracking()
            .Where(p => p.Status == "Confirmed" && p.PaymentDate >= monthStart)
            .CountAsync(cancellationToken);

        var wonWithoutInvoice = await _context.SalesRequests
            .AsNoTracking()
            .Where(r => r.Status == "Won" && r.BusinessId != null)
            .GroupJoin(
                _context.BillingInvoices.AsNoTracking(),
                sr => sr.Id,
                inv => inv.RelatedSalesRequestId,
                (sr, invs) => new { sr.Id, HasInvoice = invs.Any() })
            .CountAsync(x => !x.HasInvoice, cancellationToken);

        return new AdminBillingSummary
        {
            OpenAmount = open,
            OverdueAmount = overdueAmount,
            OverdueCount = overdueCount,
            PaidThisMonth = paidThisMonth,
            NewInvoicesThisMonth = newInvoicesThisMonth,
            ManualPaymentsThisMonth = manualPaymentsThisMonth,
            WonWithoutInvoiceCount = wonWithoutInvoice
        };
    }

    public async Task<List<BillingInvoice>> GetOverdueInvoicesAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.BillingInvoices
            .AsNoTracking()
            .Where(i => i.Status == "Overdue")
            .OrderBy(i => i.DueDate)
            .Take(Math.Max(1, take))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BillingPayment>> GetRecentPaymentsAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.BillingPayments
            .AsNoTracking()
            .Where(p => p.Status == "Confirmed")
            .OrderByDescending(p => p.PaymentDate)
            .Take(Math.Max(1, take))
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateInvoiceNumberAsync(int businessId, CancellationToken cancellationToken = default)
    {
        var prefix = $"INV-{DateTime.UtcNow:yyyyMM}-";

        var last = await _context.BillingInvoices
            .AsNoTracking()
            .Where(i => i.BusinessId == businessId && i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.Id)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var next = 1;
        if (!string.IsNullOrWhiteSpace(last))
        {
            var tail = last.Substring(prefix.Length);
            if (int.TryParse(tail, out var parsed))
            {
                next = parsed + 1;
            }
        }

        return prefix + next.ToString("D4", CultureInfo.InvariantCulture);
    }
}

