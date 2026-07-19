using DukkanPilot.Web.Api.Mobile.V1.Authorization;
using DukkanPilot.Web.Api.Mobile.V1.Common;
using DukkanPilot.Web.Api.Mobile.V1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DukkanPilot.Web.Api.Mobile.V1.Controllers;

[Route("api/mobile/v1/kitchen/orders")]
[Authorize(Policy = MobilePolicies.OwnerOrStaff)]
public sealed class MobileKitchenController : MobileApiControllerBase
{
    private readonly IMobileOrderQueryService _queries;

    public MobileKitchenController(IMobileOrderQueryService queries)
    {
        _queries = queries;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!MobilePrincipal.TryGetContext(User, out var mobileContext))
        {
            return MobileProblem(StatusCodes.Status401Unauthorized, "unauthorized", "Authentication is required.");
        }

        return Ok(await _queries.GetKitchenAsync(mobileContext.BusinessId, cancellationToken));
    }
}
