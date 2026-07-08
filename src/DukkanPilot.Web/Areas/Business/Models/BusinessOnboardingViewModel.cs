using DukkanPilot.Web.Models.Onboarding;

namespace DukkanPilot.Web.Areas.Business.Models;

public class BusinessOnboardingViewModel
{
    public CustomerOnboardingSnapshot Snapshot { get; set; } = new();
    public bool IsBusinessOwner { get; set; }
}
