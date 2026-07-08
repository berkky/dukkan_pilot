using System.Globalization;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Admin.Models;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Route("Admin/Billing")]
public class BillingController : AdminBaseController
{
    private readonly AppDbContext _context;
    private readonly IBillingOperationsService _billing;

    public BillingController(AppDbContext context, IBillingOperationsService billing)
    {
        _context = context;
        _billing = billing;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        int? businessId,
        string? status,
        string? paymentStatus,
        DateTime? dueFrom,
        DateTime? dueTo,
        string? search,
        CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "billing";

        var summary = await _billing.GetAdminBillingSummaryAsync(cancellationToken);

        var q = _context.BillingInvoices.AsNoTracking().Where(i => i.Status != "Cancelled");

        if (businessId.HasValue)
        {
            q = q.Where(i => i.BusinessId == businessId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            q = q.Where(i => i.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(paymentStatus))
        {
            q = q.Where(i => i.PaymentStatus == paymentStatus);
        }

        if (dueFrom.HasValue)
        {
            q = q.Where(i => i.DueDate >= dueFrom.Value.Date);
        }

        if (dueTo.HasValue)
        {
            q = q.Where(i => i.DueDate <= dueTo.Value.Date.AddDays(1).AddTicks(-1));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(i => i.InvoiceNumber.Contains(s) || i.Title.Contains(s));
        }

        var invoices = await q
            .OrderByDescending(i => i.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        var businessIds = invoices.Select(i => i.BusinessId).Distinct().ToList();
        var businessNames = await _context.Businesses
            .AsNoTracking()
            .Where(b => businessIds.Contains(b.Id))
            .Select(b => new { b.Id, b.Name })
            .ToListAsync(cancellationToken);

        var businessNameMap = businessNames.ToDictionary(x => x.Id, x => x.Name);

        var paidByInvoice = await _context.BillingPayments
            .AsNoTracking()
            .Where(p => p.BillingInvoiceId != null && invoices.Select(i => i.Id).Contains(p.BillingInvoiceId.Value) && p.Status == "Confirmed")
            .GroupBy(p => p.BillingInvoiceId!.Value)
            .Select(g => new { InvoiceId = g.Key, Paid = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var paidMap = paidByInvoice.ToDictionary(x => x.InvoiceId, x => x.Paid);

        var businesses = await _context.Businesses
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new AdminBusinessOptionViewModel { Id = b.Id, Name = b.Name })
            .ToListAsync(cancellationToken);

        var model = new AdminBillingViewModel
        {
            OpenAmount = summary.OpenAmount,
            OverdueAmount = summary.OverdueAmount,
            PaidThisMonth = summary.PaidThisMonth,
            NewInvoicesThisMonth = summary.NewInvoicesThisMonth,
            OverdueInvoicesCount = summary.OverdueCount,
            ManualPaymentsThisMonth = summary.ManualPaymentsThisMonth,
            WonWithoutInvoiceCount = summary.WonWithoutInvoiceCount,
            BusinessId = businessId,
            Status = status,
            PaymentStatus = paymentStatus,
            DueFrom = dueFrom,
            DueTo = dueTo,
            Search = search,
            Businesses = businesses,
            Invoices = invoices.Select(i =>
            {
                var paid = paidMap.TryGetValue(i.Id, out var p) ? p : 0m;
                var remaining = Math.Max(0m, i.TotalAmount - paid);
                return new AdminBillingInvoiceRowViewModel
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    BusinessId = i.BusinessId,
                    BusinessName = businessNameMap.TryGetValue(i.BusinessId, out var bn) ? bn : $"#{i.BusinessId}",
                    Title = i.Title,
                    DueDate = i.DueDate,
                    TotalAmount = i.TotalAmount,
                    Currency = i.Currency,
                    Status = i.Status,
                    PaymentStatus = i.PaymentStatus,
                    PaidAmount = paid,
                    RemainingAmount = remaining
                };
            }).ToList()
        };

        return View(model);
    }

    [HttpGet("CreateInvoice")]
    public async Task<IActionResult> CreateInvoice(int? businessId, int? salesRequestId, CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "billing";

        var businesses = await _context.Businesses
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new AdminBusinessOptionViewModel { Id = b.Id, Name = b.Name })
            .ToListAsync(cancellationToken);

        return View(new AdminBillingCreateInvoiceViewModel
        {
            BusinessId = businessId ?? 0,
            RelatedSalesRequestId = salesRequestId,
            Businesses = businesses
        });
    }

    [HttpPost("CreateInvoice")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInvoice(AdminBillingCreateInvoiceViewModel model, CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "billing";

        if (model.BusinessId <= 0)
        {
            ModelState.AddModelError(nameof(model.BusinessId), "İşletme seçin.");
        }

        if (string.IsNullOrWhiteSpace(model.Title))
        {
            ModelState.AddModelError(nameof(model.Title), "Başlık zorunludur.");
        }

        if (model.Amount < 0)
        {
            ModelState.AddModelError(nameof(model.Amount), "Tutar negatif olamaz.");
        }

        if (model.TaxAmount.HasValue && model.TaxAmount.Value < 0)
        {
            ModelState.AddModelError(nameof(model.TaxAmount), "Vergi negatif olamaz.");
        }

        if (model.DueDate.Date < model.IssueDate.Date)
        {
            ModelState.AddModelError(nameof(model.DueDate), "Vade tarihi, kesim tarihinden önce olamaz.");
        }

        if (!ModelState.IsValid)
        {
            model.Businesses = await _context.Businesses
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new AdminBusinessOptionViewModel { Id = b.Id, Name = b.Name })
                .ToListAsync(cancellationToken);

            return View(model);
        }

        var invoice = await _billing.CreateInvoiceAsync(new BillingInvoiceCreateInput
        {
            BusinessId = model.BusinessId,
            SubscriptionPlanId = model.SubscriptionPlanId,
            RelatedSalesRequestId = model.RelatedSalesRequestId,
            Title = model.Title,
            Description = model.Description,
            Amount = model.Amount,
            TaxAmount = model.TaxAmount,
            Currency = model.Currency,
            IssueDate = model.IssueDate,
            DueDate = model.DueDate,
            PeriodStart = model.PeriodStart,
            PeriodEnd = model.PeriodEnd,
            BusinessVisibleNote = model.BusinessVisibleNote,
            AdminNotes = model.AdminNotes,
            CreatedByUserId = TryParseInt(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value),
            CreatedByUserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
            Source = "AdminCreated"
        }, cancellationToken);

        TempData["Success"] = "Tahsilat kaydı oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = invoice.Id });
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "billing";

        var invoice = await _context.BillingInvoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        var businessName = await _context.Businesses
            .AsNoTracking()
            .Where(b => b.Id == invoice.BusinessId)
            .Select(b => b.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? $"#{invoice.BusinessId}";

        var payments = await _context.BillingPayments
            .AsNoTracking()
            .Where(p => p.BillingInvoiceId == invoice.Id)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken);

        var paid = payments.Where(p => p.Status == "Confirmed").Sum(p => p.Amount);
        var remaining = Math.Max(0m, invoice.TotalAmount - paid);

        return View(new AdminBillingDetailsViewModel
        {
            Invoice = invoice,
            BusinessName = businessName,
            Payments = payments,
            PaidAmount = paid,
            RemainingAmount = remaining
        });
    }

    [HttpGet("RecordPayment")]
    public async Task<IActionResult> RecordPayment(int? invoiceId, int? businessId, CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "billing";

        var businesses = await _context.Businesses
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new AdminBusinessOptionViewModel { Id = b.Id, Name = b.Name })
            .ToListAsync(cancellationToken);

        var model = new AdminBillingRecordPaymentViewModel
        {
            BillingInvoiceId = invoiceId,
            BusinessId = businessId ?? 0,
            Businesses = businesses
        };

        if (invoiceId.HasValue)
        {
            var inv = await _context.BillingInvoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == invoiceId.Value, cancellationToken);
            if (inv is not null)
            {
                model.BusinessId = inv.BusinessId;
                model.InvoiceNumber = inv.InvoiceNumber;
                model.InvoiceTotalAmount = inv.TotalAmount;
                model.InvoiceCurrency = inv.Currency;
                model.Currency = inv.Currency;
            }

            model.BusinessName = await _context.Businesses.AsNoTracking()
                .Where(b => b.Id == model.BusinessId)
                .Select(b => b.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return View(model);
    }

    [HttpPost("RecordPayment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordPayment(AdminBillingRecordPaymentViewModel model, CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "billing";

        if (model.BusinessId <= 0)
        {
            ModelState.AddModelError(nameof(model.BusinessId), "İşletme seçin.");
        }

        if (model.Amount <= 0)
        {
            ModelState.AddModelError(nameof(model.Amount), "Tutar 0'dan büyük olmalıdır.");
        }

        if (!ModelState.IsValid)
        {
            model.Businesses = await _context.Businesses
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new AdminBusinessOptionViewModel { Id = b.Id, Name = b.Name })
                .ToListAsync(cancellationToken);

            return View(model);
        }

        var payment = await _billing.RecordPaymentAsync(new BillingPaymentCreateInput
        {
            BusinessId = model.BusinessId,
            BillingInvoiceId = model.BillingInvoiceId,
            Amount = model.Amount,
            Currency = model.Currency,
            PaymentDate = model.PaymentDate,
            Method = model.Method,
            Status = model.Status,
            ReferenceNumber = model.ReferenceNumber,
            PayerName = model.PayerName,
            BusinessVisibleNote = model.BusinessVisibleNote,
            AdminNotes = model.AdminNotes,
            RecordedByUserId = TryParseInt(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value),
            RecordedByUserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
        }, cancellationToken);

        TempData["Success"] = "Ödeme kaydı işlendi. Not: Ödeme kaydı aboneliği otomatik uzatmaz; gerekirse abonelik ekranından güncelleyin.";

        if (model.BillingInvoiceId.HasValue)
        {
            return RedirectToAction(nameof(Details), new { id = model.BillingInvoiceId.Value });
        }

        return RedirectToAction(nameof(Payments));
    }

    [HttpPost("CancelInvoice/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelInvoice(int id, CancellationToken cancellationToken)
    {
        await _billing.CancelInvoiceAsync(
            invoiceId: id,
            adminUserId: TryParseInt(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value),
            adminEmail: User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
            cancellationToken: cancellationToken);

        TempData["Success"] = "Tahsilat kaydı iptal edildi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("Payments")]
    public async Task<IActionResult> Payments(int? businessId, string? method, string? status, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "billing";

        var q = _context.BillingPayments.AsNoTracking();

        if (businessId.HasValue)
        {
            q = q.Where(p => p.BusinessId == businessId.Value);
        }

        if (!string.IsNullOrWhiteSpace(method))
        {
            q = q.Where(p => p.Method == method);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            q = q.Where(p => p.Status == status);
        }

        if (from.HasValue)
        {
            q = q.Where(p => p.PaymentDate >= from.Value.Date);
        }

        if (to.HasValue)
        {
            q = q.Where(p => p.PaymentDate <= to.Value.Date.AddDays(1).AddTicks(-1));
        }

        var payments = await q
            .OrderByDescending(p => p.PaymentDate)
            .Take(200)
            .ToListAsync(cancellationToken);

        var businesses = await _context.Businesses
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new AdminBusinessOptionViewModel { Id = b.Id, Name = b.Name })
            .ToListAsync(cancellationToken);

        return View(new AdminBillingPaymentsViewModel
        {
            BusinessId = businessId,
            Method = method,
            Status = status,
            From = from,
            To = to,
            Payments = payments,
            Businesses = businesses
        });
    }

    private static int? TryParseInt(string? value)
        => int.TryParse(value, out var parsed) ? parsed : null;
}

