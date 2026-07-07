namespace DukkanPilot.Web.Areas.Business.Models;

public class RewardListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RequiredPoints { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
