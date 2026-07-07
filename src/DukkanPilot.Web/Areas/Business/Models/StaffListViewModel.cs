using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Areas.Business.Models;

public class StaffListViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public BusinessRole BusinessRole { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
