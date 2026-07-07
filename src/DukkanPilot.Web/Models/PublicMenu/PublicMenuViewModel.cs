namespace DukkanPilot.Web.Models.PublicMenu;

public class PublicMenuViewModel
{
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? LogoUrl { get; set; }
    public string ThemeColor { get; set; } = "#2563eb";
    public string Currency { get; set; } = "TRY";
    public string? WhatsAppNumber { get; set; }

    public List<PublicMenuCampaignViewModel> Campaigns { get; set; } = new();
    public List<PublicMenuCategoryViewModel> Categories { get; set; } = new();
}
