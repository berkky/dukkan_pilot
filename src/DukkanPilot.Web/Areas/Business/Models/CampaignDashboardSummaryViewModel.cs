namespace DukkanPilot.Web.Areas.Business.Models;

public class CampaignDashboardSummaryViewModel
{
    public int TotalCampaignCount { get; set; }
    public int ActiveCampaignCount { get; set; }
    public int PublishedCampaignCount { get; set; }
    public string? NearestEndingCampaignTitle { get; set; }
    public DateTime? NearestEndingCampaignEndDate { get; set; }
}
