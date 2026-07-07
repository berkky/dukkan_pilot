namespace DukkanPilot.Web.Areas.Business.Models;

public class QrMenuViewModel
{
    public string BusinessName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string PublicMenuUrl { get; set; } = string.Empty;
    public string PublicMenuPath => $"/m/{Slug}";
    public string WhatsAppShareUrl { get; set; } = string.Empty;
    public string ThemeColor { get; set; } = "#2563eb";
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public bool HasQrCode { get; set; }
    public DateTime? LastGeneratedAt { get; set; }
    public string QrDownloadFileName => $"dukkanpilot-{Slug}-qr.png";
    public int QrCodesUsed { get; set; }
    public bool QrLimitReached { get; set; }
    public string? QrLimitMessage { get; set; }
}
