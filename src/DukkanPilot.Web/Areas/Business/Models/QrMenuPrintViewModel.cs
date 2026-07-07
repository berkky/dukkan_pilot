namespace DukkanPilot.Web.Areas.Business.Models;

public class QrMenuPrintViewModel
{
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessSlug { get; set; } = string.Empty;
    public string PublicMenuUrl { get; set; } = string.Empty;
    public string PublicMenuPath => $"/m/{BusinessSlug}";
    public string QrPayload => PublicMenuUrl;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string ThemeColor { get; set; } = "#2563eb";
    public string Currency { get; set; } = "TRY";
    public string PrintTitle { get; set; } = string.Empty;
    public string PrintSubtitle { get; set; } = "Dijital Menü";
}
