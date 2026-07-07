namespace DukkanPilot.Web.Areas.Business.Models;

public class RewardRedemptionHistoryViewModel
{
    public DateTime CreatedAt { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public int Points { get; set; }
    public string? Description { get; set; }
}
