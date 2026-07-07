namespace DukkanPilot.Web.Areas.Business.Models;

public class CustomerListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int TotalPoints { get; set; }
    public bool IsActive { get; set; }
    public int OrderCount { get; set; }
    public DateTime? LastOrderDate { get; set; }
}
