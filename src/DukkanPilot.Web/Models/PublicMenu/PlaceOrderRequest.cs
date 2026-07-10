namespace DukkanPilot.Web.Models.PublicMenu;

public class PlaceOrderRequest
{
    public List<PlaceOrderItemRequest> Items { get; set; } = new();
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Notes { get; set; }
    public string? RewardRequestName { get; set; }
    public string? TablePublicCode { get; set; }
}
