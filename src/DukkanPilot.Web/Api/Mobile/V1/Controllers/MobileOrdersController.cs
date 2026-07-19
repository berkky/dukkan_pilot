using DukkanPilot.Core.Enums;
using DukkanPilot.Web.Api.Mobile.V1.Authorization;
using DukkanPilot.Web.Api.Mobile.V1.Common;
using DukkanPilot.Web.Api.Mobile.V1.Contracts.Orders;
using DukkanPilot.Web.Api.Mobile.V1.Services;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DukkanPilot.Web.Api.Mobile.V1.Controllers;

[Route("api/mobile/v1/orders")]
[Authorize(Policy = MobilePolicies.OwnerOrStaff)]
public sealed class MobileOrdersController : MobileApiControllerBase
{
    private const int MaximumPageSize = 100;
    private readonly IMobileOrderQueryService _queries;
    private readonly IOrderStatusService _statusService;

    public MobileOrdersController(
        IMobileOrderQueryService queries,
        IOrderStatusService statusService)
    {
        _queries = queries;
        _statusService = statusService;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        string? serviceType = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetMobileContext(out var mobileContext, out var failure))
        {
            return failure!;
        }

        OrderStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            {
                return MobileProblem(
                    StatusCodes.Status400BadRequest,
                    "invalid_order_status",
                    "The order status filter is invalid.");
            }

            statusFilter = parsed;
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaximumPageSize);
        var response = await _queries.GetPageAsync(
            mobileContext.BusinessId,
            new MobileOrderQuery(page, pageSize, statusFilter, fromUtc, toUtc, serviceType),
            cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        if (!TryGetMobileContext(out var mobileContext, out var failure))
        {
            return failure!;
        }

        var order = await _queries.GetDetailsAsync(mobileContext.BusinessId, id, cancellationToken);
        return order is null
            ? MobileProblem(StatusCodes.Status404NotFound, "resource_not_found", "The order was not found.")
            : Ok(order);
    }

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        MobileOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetMobileContext(out var mobileContext, out var failure))
        {
            return failure!;
        }

        if (!Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var targetStatus) ||
            !Enum.IsDefined(targetStatus))
        {
            return MobileProblem(
                StatusCodes.Status400BadRequest,
                "invalid_order_status",
                "The order status is invalid.");
        }

        var result = await _statusService.ChangeAsync(
            mobileContext.BusinessId,
            id,
            targetStatus,
            cancellationToken);

        if (result.Failure == OrderStatusChangeFailure.NotFound)
        {
            return MobileProblem(StatusCodes.Status404NotFound, "resource_not_found", "The order was not found.");
        }

        if (result.Failure is OrderStatusChangeFailure.InvalidStatus or OrderStatusChangeFailure.InvalidTransition)
        {
            return MobileProblem(
                StatusCodes.Status400BadRequest,
                "invalid_order_status",
                "The requested order status transition is not allowed.");
        }

        var order = await _queries.GetDetailsAsync(mobileContext.BusinessId, id, cancellationToken);
        return Ok(order);
    }

    private bool TryGetMobileContext(
        out MobileRequestContext mobileContext,
        out ObjectResult? failure)
    {
        if (MobilePrincipal.TryGetContext(User, out mobileContext))
        {
            failure = null;
            return true;
        }

        failure = MobileProblem(StatusCodes.Status401Unauthorized, "unauthorized", "Authentication is required.");
        return false;
    }
}
