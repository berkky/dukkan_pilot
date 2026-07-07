namespace DukkanPilot.Web.Areas.Business.Models;

public class RewardDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RequiredPoints { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RewardRedemptionHistoryViewModel> RecentRedemptions { get; set; } = new();
}
