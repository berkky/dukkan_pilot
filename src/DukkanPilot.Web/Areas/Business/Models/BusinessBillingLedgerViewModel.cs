using DukkanPilot.Core.Entities;

namespace DukkanPilot.Web.Areas.Business.Models;

public class BusinessBillingLedgerViewModel
{
    public decimal OpenAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal PaidThisMonth { get; set; }
    public DateTime? NextDueDateUtc { get; set; }

    public List<BillingInvoice> Invoices { get; set; } = new();
    public List<BillingPayment> Payments { get; set; } = new();

    public string DisclaimerText { get; set; } =
        "Bu ekran ödeme/tahsilat takibi içindir. Resmi fatura/e-Belge yerine geçmez; resmi süreçler muhasebe/e-Belge kanalından yürütülmelidir.";
}

