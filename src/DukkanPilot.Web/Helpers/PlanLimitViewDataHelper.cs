using DukkanPilot.Core.Enums;
using DukkanPilot.Web.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace DukkanPilot.Web.Helpers;

public static class PlanLimitViewDataHelper
{
    public static async Task SetLimitWarningAsync(
        Controller controller,
        BusinessPlanLimitHelper planLimitHelper,
        int businessId,
        PlanLimitResource resource)
    {
        if (!await planLimitHelper.IsLimitReachedAsync(businessId, resource))
        {
            return;
        }

        var isBusinessOwner = controller.User.IsInRole(nameof(UserRole.BusinessOwner));
        controller.ViewData["LimitWarning"] = planLimitHelper.GetLimitReachedMessage(resource, isBusinessOwner);
        controller.ViewData["IsBusinessOwner"] = isBusinessOwner;
    }
}
