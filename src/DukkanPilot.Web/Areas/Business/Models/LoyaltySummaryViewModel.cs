namespace DukkanPilot.Web.Areas.Business.Models;

public class LoyaltySummaryViewModel
{
    public int TotalActiveCustomerPoints { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    public bool HasActiveLoyaltyRule { get; set; }
}
