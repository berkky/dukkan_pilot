using System.Globalization;

namespace DukkanPilot.Web.Models.PublicMenu;

public class PublicMenuViewModel
{
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? LogoUrl { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
    public string ThemeColor { get; set; } = "#2563eb";
    public string Currency { get; set; } = "TRY";
    public string? WhatsAppNumber { get; set; }

    public List<PublicMenuCampaignViewModel> Campaigns { get; set; } = new();

    public List<PublicRewardViewModel> Rewards { get; set; } = new();

    public bool HasActiveCampaigns => Campaigns.Count > 0;

    public bool HasRewards => Rewards.Count > 0;

    public List<PublicMenuCategoryViewModel> Categories { get; set; } = new();

    public string FormatPrice(decimal price)
    {
        var formatted = price.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"));
        return Currency.Equals("TRY", StringComparison.OrdinalIgnoreCase)
            ? $"{formatted} ₺"
            : $"{formatted} {Currency}";
    }
}
