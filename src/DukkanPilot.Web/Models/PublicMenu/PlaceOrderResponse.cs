namespace DukkanPilot.Web.Models.PublicMenu;

public class PlaceOrderResponse
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string WhatsAppUrl { get; set; } = string.Empty;
    public string ConfirmationUrl { get; set; } = string.Empty;
    public string TrackingUrl { get; set; } = string.Empty;
}
