namespace DukkanPilot.Web.Models.PublicMenu;

public class PublicRewardViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int RequiredPoints { get; set; }
}
