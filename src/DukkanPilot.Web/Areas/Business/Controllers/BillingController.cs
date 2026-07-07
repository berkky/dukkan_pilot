using DukkanPilot.Core.Enums;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Billing")]
public class BillingController : BusinessBaseController
{
    private readonly BusinessSubscriptionStatusHelper _subscriptionStatusHelper;

    public BillingController(BusinessSubscriptionStatusHelper subscriptionStatusHelper)
    {
        _subscriptionStatusHelper = subscriptionStatusHelper;
    }

    [HttpGet("")]
    [Authorize(Roles = nameof(UserRole.BusinessOwner))]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "billing";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var model = await _subscriptionStatusHelper.GetStatusAsync(businessId);
        return View(model);
    }

    [HttpGet("Required")]
    public IActionResult Required()
    {
        ViewData["ActiveMenu"] = "billing-required";
        ViewData["IsBusinessOwner"] = User.IsInRole(nameof(UserRole.BusinessOwner));
        return View();
    }
}
