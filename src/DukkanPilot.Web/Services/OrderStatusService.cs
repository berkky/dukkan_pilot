using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Services;

public enum OrderStatusChangeFailure
{
    None,
    NotFound,
    InvalidStatus,
    InvalidTransition
}

public sealed record OrderStatusChangeResult(
    OrderStatusChangeFailure Failure,
    int? OrderId = null,
    OrderStatus? PreviousStatus = null,
    OrderStatus? CurrentStatus = null,
    string? Message = null)
{
    public bool Succeeded => Failure == OrderStatusChangeFailure.None;
}

public interface IOrderStatusService
{
    Task<OrderStatusChangeResult> ChangeAsync(
        int businessId,
        int orderId,
        OrderStatus targetStatus,
        CancellationToken cancellationToken);
}

public sealed class OrderStatusService : IOrderStatusService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly INotificationService _notifications;

    public OrderStatusService(
        AppDbContext context,
        IAuditLogService auditLog,
        INotificationService notifications)
    {
        _context = context;
        _auditLog = auditLog;
        _notifications = notifications;
    }

    public async Task<OrderStatusChangeResult> ChangeAsync(
        int businessId,
        int orderId,
        OrderStatus targetStatus,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(targetStatus))
        {
            return new OrderStatusChangeResult(OrderStatusChangeFailure.InvalidStatus);
        }

        var order = await _context.Orders.FirstOrDefaultAsync(
            candidate => candidate.Id == orderId && candidate.BusinessId == businessId,
            cancellationToken);

        if (order is null)
        {
            return new OrderStatusChangeResult(OrderStatusChangeFailure.NotFound);
        }

        var previousStatus = order.Status;
        if (!IsTransitionAllowed(previousStatus, targetStatus))
        {
            return new OrderStatusChangeResult(
                OrderStatusChangeFailure.InvalidTransition,
                order.Id,
                previousStatus,
                targetStatus);
        }

        var isTransitionToCompleted = targetStatus == OrderStatus.Completed &&
                                      previousStatus != OrderStatus.Completed;
        string? loyaltyMessage = null;

        if (previousStatus != targetStatus)
        {
            order.Status = targetStatus;
            order.UpdatedAt = DateTime.UtcNow;

            if (isTransitionToCompleted)
            {
                loyaltyMessage = (await TryAwardCompletionPointsAsync(
                    order,
                    businessId,
                    cancellationToken)).Message;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await WriteSideEffectsAsync(order, previousStatus, targetStatus, isTransitionToCompleted);
        }

        return new OrderStatusChangeResult(
            OrderStatusChangeFailure.None,
            order.Id,
            previousStatus,
            targetStatus,
            loyaltyMessage ?? $"Order status changed to {OrderDisplayHelper.GetStatusLabel(targetStatus)}.");
    }

    private static bool IsTransitionAllowed(OrderStatus current, OrderStatus target)
    {
        if (current == target)
        {
            return true;
        }

        return current switch
        {
            OrderStatus.Pending => target is OrderStatus.Preparing or OrderStatus.Cancelled,
            OrderStatus.Preparing => target is OrderStatus.Completed or OrderStatus.Cancelled,
            _ => false
        };
    }

    private async Task WriteSideEffectsAsync(
        Order order,
        OrderStatus previousStatus,
        OrderStatus targetStatus,
        bool loyaltyTriggered)
    {
        var summary = loyaltyTriggered
            ? $"Order status changed: {previousStatus} -> {targetStatus} (loyalty flow triggered)."
            : $"Order status changed: {previousStatus} -> {targetStatus}.";

        await _auditLog.LogBusinessAsync(
            order.BusinessId,
            "Order.StatusChanged",
            "Order",
            order.Id,
            summary,
            new
            {
                orderId = order.Id,
                oldStatus = previousStatus.ToString(),
                newStatus = targetStatus.ToString(),
                totalAmount = order.TotalAmount
            });

        if (targetStatus == OrderStatus.Preparing)
        {
            await NotifyAsync(order, previousStatus, targetStatus, "OrderStatusChanged", "Order is being prepared", "Info");
        }
        else if (targetStatus == OrderStatus.Completed)
        {
            await NotifyAsync(order, previousStatus, targetStatus, "OrderCompleted", "Order completed", "Success");
        }
        else if (targetStatus == OrderStatus.Cancelled)
        {
            await NotifyAsync(order, previousStatus, targetStatus, "OrderCancelled", "Order cancelled", "Warning");
        }
    }

    private Task NotifyAsync(
        Order order,
        OrderStatus previousStatus,
        OrderStatus targetStatus,
        string type,
        string title,
        string severity)
    {
        return _notifications.CreateBusinessAsync(
            order.BusinessId,
            type,
            title,
            $"Order status changed: {previousStatus} -> {targetStatus} (#{order.OrderNumber}).",
            $"/Business/Orders/Details/{order.Id}",
            severity,
            "Order",
            order.Id,
            new { orderId = order.Id, oldStatus = previousStatus.ToString(), newStatus = targetStatus.ToString() },
            allowDuplicate: true);
    }

    private async Task<LoyaltyAwardResult> TryAwardCompletionPointsAsync(
        Order order,
        int businessId,
        CancellationToken cancellationToken)
    {
        var customer = await ResolveCustomerAsync(order, businessId, cancellationToken);
        if (customer is null)
        {
            return LoyaltyAwardResult.NoCustomer();
        }

        var activeRule = await _context.LoyaltyRules
            .Where(rule => rule.BusinessId == businessId && rule.IsActive && rule.PointsPerAmount > 0)
            .OrderByDescending(rule => rule.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeRule is null)
        {
            return LoyaltyAwardResult.NoAward();
        }

        var earnedPoints = OrderLoyaltyHelper.CalculateEarnedPoints(order.TotalAmount, activeRule.PointsPerAmount);
        if (earnedPoints <= 0)
        {
            return LoyaltyAwardResult.NoAward();
        }

        var description = OrderLoyaltyHelper.BuildCompletionDescription(order.OrderNumber);
        var alreadyAwarded = await _context.LoyaltyTransactions.AnyAsync(
            transaction => transaction.BusinessId == businessId &&
                           transaction.CustomerId == customer.Id &&
                           transaction.Type == LoyaltyTransactionType.Earn &&
                           transaction.Description == description,
            cancellationToken);

        if (alreadyAwarded)
        {
            return LoyaltyAwardResult.NoAward();
        }

        customer.TotalPoints += earnedPoints;
        customer.UpdatedAt = DateTime.UtcNow;
        _context.LoyaltyTransactions.Add(new LoyaltyTransaction
        {
            BusinessId = businessId,
            CustomerId = customer.Id,
            Points = earnedPoints,
            Type = LoyaltyTransactionType.Earn,
            Description = description
        });

        return LoyaltyAwardResult.Awarded(earnedPoints);
    }

    private async Task<Customer?> ResolveCustomerAsync(
        Order order,
        int businessId,
        CancellationToken cancellationToken)
    {
        if (order.CustomerId.HasValue)
        {
            var byId = await _context.Customers.FirstOrDefaultAsync(
                customer => customer.Id == order.CustomerId.Value && customer.BusinessId == businessId,
                cancellationToken);
            if (byId is not null)
            {
                return byId;
            }
        }

        if (string.IsNullOrWhiteSpace(order.CustomerPhone))
        {
            return null;
        }

        var normalizedPhone = order.CustomerPhone.Trim();
        return await _context.Customers.FirstOrDefaultAsync(
            customer => customer.BusinessId == businessId && customer.Phone == normalizedPhone,
            cancellationToken);
    }

    private sealed record LoyaltyAwardResult(string? Message)
    {
        public static LoyaltyAwardResult Awarded(int points) =>
            new($"Order completed and {points} loyalty points were awarded.");

        public static LoyaltyAwardResult NoCustomer() =>
            new("Order completed. Loyalty points were not awarded because no customer record was found.");

        public static LoyaltyAwardResult NoAward() => new((string?)null);
    }
}
