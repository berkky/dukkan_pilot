namespace DukkanPilot.Web.Areas.Business.Models;

public class CampaignListViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsPublished { get; set; }
}
