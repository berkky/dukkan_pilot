using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Api.Mobile.V1.Contracts.Common;
using DukkanPilot.Web.Api.Mobile.V1.Contracts.Orders;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Api.Mobile.V1.Services;

public sealed record MobileOrderQuery(
    int Page,
    int PageSize,
    OrderStatus? Status,
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? ServiceType);

public interface IMobileOrderQueryService
{
    Task<PagedResponse<MobileOrderListItem>> GetPageAsync(
        int businessId,
        MobileOrderQuery query,
        CancellationToken cancellationToken);

    Task<MobileOrderDetails?> GetDetailsAsync(
        int businessId,
        int orderId,
        CancellationToken cancellationToken);

    Task<MobileKitchenResponse> GetKitchenAsync(int businessId, CancellationToken cancellationToken);

    Task<MobileDashboardTodayResponse> GetTodayAsync(int businessId, CancellationToken cancellationToken);
}

public sealed class MobileOrderQueryService : IMobileOrderQueryService
{
    private readonly AppDbContext _context;

    public MobileOrderQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<MobileOrderListItem>> GetPageAsync(
        int businessId,
        MobileOrderQuery query,
        CancellationToken cancellationToken)
    {
        var orders = _context.Orders.AsNoTracking().Where(order => order.BusinessId == businessId);
        if (query.Status.HasValue)
        {
            orders = orders.Where(order => order.Status == query.Status.Value);
        }

        if (query.FromUtc.HasValue)
        {
            orders = orders.Where(order => order.CreatedAt >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            orders = orders.Where(order => order.CreatedAt < query.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.ServiceType))
        {
            var serviceType = query.ServiceType.Trim();
            orders = orders.Where(order => order.ServiceType == serviceType);
        }

        var totalCount = await orders.CountAsync(cancellationToken);
        var rows = await orders
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(order => new
            {
                order.Id,
                order.OrderNumber,
                order.TotalAmount,
                order.Status,
                order.Source,
                order.ServiceType,
                order.TableLabelSnapshot,
                order.CustomerName,
                order.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(row => new MobileOrderListItem(
            row.Id,
            row.OrderNumber,
            row.TotalAmount,
            row.Status.ToString(),
            row.Source.ToString(),
            row.ServiceType,
            row.TableLabelSnapshot,
            row.CustomerName,
            row.CreatedAt)).ToArray();

        return new PagedResponse<MobileOrderListItem>(query.Page, query.PageSize, totalCount, items);
    }

    public async Task<MobileOrderDetails?> GetDetailsAsync(
        int businessId,
        int orderId,
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == orderId && candidate.BusinessId == businessId,
                cancellationToken);

        return order is null ? null : MapDetails(order);
    }

    public async Task<MobileKitchenResponse> GetKitchenAsync(
        int businessId,
        CancellationToken cancellationToken)
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Where(order => order.BusinessId == businessId &&
                            (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Preparing))
            .OrderBy(order => order.Status)
            .ThenBy(order => order.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return new MobileKitchenResponse(
            orders.Count(order => order.Status == OrderStatus.Pending),
            orders.Count(order => order.Status == OrderStatus.Preparing),
            orders.Select(MapDetails).ToArray(),
            DateTime.UtcNow);
    }

    public async Task<MobileDashboardTodayResponse> GetTodayAsync(
        int businessId,
        CancellationToken cancellationToken)
    {
        var startUtc = DateTime.UtcNow.Date;
        var endUtc = startUtc.AddDays(1);
        var orders = _context.Orders.AsNoTracking().Where(
            order => order.BusinessId == businessId &&
                     order.CreatedAt >= startUtc &&
                     order.CreatedAt < endUtc);

        return new MobileDashboardTodayResponse(
            await orders.CountAsync(cancellationToken),
            await orders.CountAsync(order => order.Status == OrderStatus.Pending, cancellationToken),
            await orders.CountAsync(order => order.Status == OrderStatus.Preparing, cancellationToken),
            await orders.CountAsync(order => order.Status == OrderStatus.Completed, cancellationToken),
            await orders.CountAsync(order => order.Status == OrderStatus.Cancelled, cancellationToken),
            await orders.Where(order => order.Status != OrderStatus.Cancelled)
                .SumAsync(order => order.TotalAmount, cancellationToken),
            DateTime.UtcNow);
    }

    private static MobileOrderDetails MapDetails(Order order)
    {
        return new MobileOrderDetails(
            order.Id,
            order.OrderNumber,
            order.SubtotalAmount,
            order.DiscountAmount,
            order.TotalAmount,
            order.Status.ToString(),
            order.Source.ToString(),
            order.ServiceType,
            order.TableLabelSnapshot,
            order.CustomerName,
            order.CustomerPhone,
            order.Notes,
            order.CreatedAt,
            order.Items.OrderBy(item => item.Id)
                .Select(item => new MobileOrderItem(
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice,
                    item.UnitPrice * item.Quantity))
                .ToArray());
    }
}
