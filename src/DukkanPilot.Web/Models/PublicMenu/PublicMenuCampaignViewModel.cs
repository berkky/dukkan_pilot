namespace DukkanPilot.Web.Models.PublicMenu;

public class PublicMenuCampaignViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? ImageUrl { get; set; }

    public bool HasActiveCampaignPeriod => true;
}
