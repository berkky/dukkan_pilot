namespace DukkanPilot.Web.Areas.Admin.Models;

public class BusinessListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public string PlanName { get; set; } = "-";
    public DateTime CreatedAt { get; set; }
}
