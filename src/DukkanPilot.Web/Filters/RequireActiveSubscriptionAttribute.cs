using DukkanPilot.Web.Constants;
using DukkanPilot.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DukkanPilot.Web.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireActiveSubscriptionAttribute : TypeFilterAttribute
{
    public RequireActiveSubscriptionAttribute()
        : base(typeof(RequireActiveSubscriptionFilter))
    {
    }
}

public sealed class RequireActiveSubscriptionFilter : IAsyncActionFilter
{
    private readonly BusinessSubscriptionStatusHelper _subscriptionStatusHelper;

    public RequireActiveSubscriptionFilter(BusinessSubscriptionStatusHelper subscriptionStatusHelper)
    {
        _subscriptionStatusHelper = subscriptionStatusHelper;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var businessId = GetCurrentBusinessId(context);
        if (businessId is null)
        {
            await next();
            return;
        }

        if (!await _subscriptionStatusHelper.HasValidSubscriptionAsync(businessId.Value))
        {
            context.Result = new RedirectToActionResult(
                "Required",
                "Billing",
                new { area = "Business" });
            return;
        }

        await next();
    }

    private static int? GetCurrentBusinessId(ActionExecutingContext context)
    {
        var claimValue = context.HttpContext.User.FindFirst(AuthClaimTypes.BusinessId)?.Value;
        if (!int.TryParse(claimValue, out var businessId) || businessId <= 0)
        {
            return null;
        }

        return businessId;
    }
}
