using DukkanPilot.Web.Models.Success;

namespace DukkanPilot.Web.Areas.Business.Models;

public class BusinessSuccessViewModel
{
    public CustomerSuccessSnapshot Snapshot { get; set; } = new();
    public bool IsBusinessOwner { get; set; }
}
