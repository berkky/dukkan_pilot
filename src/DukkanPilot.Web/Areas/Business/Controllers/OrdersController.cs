using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Filters;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Orders")]
[RequireActiveSubscription]
public class OrdersController : BusinessBaseController
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;

    public OrdersController(AppDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? status, string? period, string? search)
    {
        ViewData["ActiveMenu"] = "orders";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var (todayStartUtc, todayEndUtc) = OrderQueryHelper.GetTodayUtcRange();
        var weekStartUtc = OrderQueryHelper.GetWeekStartUtc();
        var baseQuery = _context.Orders.AsNoTracking().Where(o => o.BusinessId == businessId);

        var latestOrderSnapshot = await baseQuery
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new { o.Id, o.CreatedAt })
            .FirstOrDefaultAsync();

        var model = new OrderIndexViewModel
        {
            StatusFilter = status,
            PeriodFilter = string.IsNullOrWhiteSpace(period) ? "all" : period.Trim().ToLowerInvariant(),
            Search = search?.Trim(),
            Summary = new OrderSummaryViewModel
            {
                TodayOrderCount = await baseQuery.CountAsync(o =>
                    o.CreatedAt >= todayStartUtc && o.CreatedAt < todayEndUtc),
                PendingCount = await baseQuery.CountAsync(o => o.Status == OrderStatus.Pending),
                PreparingCount = await baseQuery.CountAsync(o => o.Status == OrderStatus.Preparing),
                TodayRevenue = await baseQuery
                    .Where(o => o.CreatedAt >= todayStartUtc && o.CreatedAt < todayEndUtc)
                    .SumAsync(o => o.TotalAmount),
                LatestOrderId = latestOrderSnapshot?.Id,
                LatestOrderCreatedAt = latestOrderSnapshot?.CreatedAt
            }
        };

        var filteredQuery = baseQuery;

        if (Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var statusFilter) &&
            Enum.IsDefined(statusFilter))
        {
            filteredQuery = filteredQuery.Where(o => o.Status == statusFilter);
        }

        filteredQuery = model.PeriodFilter switch
        {
            "today" => filteredQuery.Where(o => o.CreatedAt >= todayStartUtc && o.CreatedAt < todayEndUtc),
            "week" => filteredQuery.Where(o => o.CreatedAt >= weekStartUtc),
            _ => filteredQuery
        };

        if (!string.IsNullOrWhiteSpace(model.Search))
        {
            var term = model.Search;
            filteredQuery = filteredQuery.Where(o =>
                o.OrderNumber.Contains(term) ||
                (o.CustomerName != null && o.CustomerName.Contains(term)) ||
                (o.CustomerPhone != null && o.CustomerPhone.Contains(term)));
        }

        model.Orders = await filteredQuery
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderListViewModel
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.CustomerName,
                CustomerPhone = o.CustomerPhone,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                Source = o.Source,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        return View(model);
    }

    [HttpGet("LiveSummary")]
    [ResponseCache(NoStore = true, Duration = 0)]
    public async Task<IActionResult> LiveSummary()
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var (todayStartUtc, todayEndUtc) = OrderQueryHelper.GetTodayUtcRange();
        var baseQuery = _context.Orders.AsNoTracking().Where(o => o.BusinessId == businessId);

        var latestOrder = await baseQuery
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                o.CreatedAt,
                o.CustomerName,
                o.TotalAmount,
                o.Status
            })
            .FirstOrDefaultAsync();

        var model = new OrderLiveSummaryViewModel
        {
            PendingCount = await baseQuery.CountAsync(o => o.Status == OrderStatus.Pending),
            PreparingCount = await baseQuery.CountAsync(o => o.Status == OrderStatus.Preparing),
            TodayOrderCount = await baseQuery.CountAsync(o =>
                o.CreatedAt >= todayStartUtc && o.CreatedAt < todayEndUtc),
            TodayRevenue = await baseQuery
                .Where(o => o.CreatedAt >= todayStartUtc && o.CreatedAt < todayEndUtc)
                .SumAsync(o => o.TotalAmount),
            LatestOrderId = latestOrder?.Id,
            LatestOrderCreatedAt = latestOrder?.CreatedAt,
            LatestOrderCustomerName = latestOrder?.CustomerName,
            LatestOrderTotal = latestOrder?.TotalAmount,
            LatestOrderStatus = latestOrder?.Status.ToString(),
            LatestOrderStatusText = latestOrder is not null
                ? OrderDisplayHelper.GetStatusLabel(latestOrder.Status)
                : null,
            LatestOrderStatusBadgeClass = latestOrder is not null
                ? OrderDisplayHelper.GetStatusBadgeClass(latestOrder.Status)
                : null,
            ServerTime = DateTime.UtcNow
        };

        return Json(model);
    }

    [HttpGet("Kitchen")]
    public async Task<IActionResult> Kitchen()
    {
        ViewData["ActiveMenu"] = "kitchen";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var (todayStartUtc, todayEndUtc) = OrderQueryHelper.GetTodayUtcRange();
        var baseQuery = _context.Orders.AsNoTracking().Where(o => o.BusinessId == businessId);

        var latestOrderSnapshot = await baseQuery
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new { o.Id, o.CreatedAt })
            .FirstOrDefaultAsync();

        var model = new OrderKitchenViewModel
        {
            PendingCount = await baseQuery.CountAsync(o => o.Status == OrderStatus.Pending),
            PreparingCount = await baseQuery.CountAsync(o => o.Status == OrderStatus.Preparing),
            CompletedTodayCount = await baseQuery.CountAsync(o =>
                o.Status == OrderStatus.Completed &&
                o.CreatedAt >= todayStartUtc &&
                o.CreatedAt < todayEndUtc),
            TodayRevenue = await baseQuery
                .Where(o => o.CreatedAt >= todayStartUtc && o.CreatedAt < todayEndUtc)
                .SumAsync(o => o.TotalAmount),
            LatestOrderId = latestOrderSnapshot?.Id,
            LatestOrderCreatedAt = latestOrderSnapshot?.CreatedAt,
            PendingOrders = await MapKitchenOrdersAsync(
                baseQuery.Where(o => o.Status == OrderStatus.Pending)),
            PreparingOrders = await MapKitchenOrdersAsync(
                baseQuery.Where(o => o.Status == OrderStatus.Preparing)),
            CompletedTodayOrders = await MapKitchenOrdersAsync(
                baseQuery.Where(o =>
                    o.Status == OrderStatus.Completed &&
                    o.CreatedAt >= todayStartUtc &&
                    o.CreatedAt < todayEndUtc)),
            CancelledTodayOrders = await MapKitchenOrdersAsync(
                baseQuery.Where(o =>
                    o.Status == OrderStatus.Cancelled &&
                    o.CreatedAt >= todayStartUtc &&
                    o.CreatedAt < todayEndUtc))
        };

        return View(model);
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveMenu"] = "orders";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var order = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == id && o.BusinessId == businessId)
            .Select(o => new OrderDetailsViewModel
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status,
                Source = o.Source,
                CustomerName = o.CustomerName,
                CustomerPhone = o.CustomerPhone,
                Notes = o.Notes,
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt,
                Items = o.Items
                    .OrderBy(i => i.Id)
                    .Select(i => new OrderItemViewModel
                    {
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (order is null)
        {
            return NotFound();
        }

        var orderEntity = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id && o.BusinessId == businessId);

        if (orderEntity is not null)
        {
            await ApplyLoyaltyInfoAsync(order, orderEntity, businessId);
        }

        return View(order);
    }

    [HttpPost("UpdateStatus/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        int id,
        OrderStatus status,
        string? returnTo = null,
        string? statusFilter = null,
        string? period = null,
        string? search = null)
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (!Enum.IsDefined(status) ||
            status is not (OrderStatus.Pending or OrderStatus.Preparing or OrderStatus.Completed or OrderStatus.Cancelled))
        {
            TempData["Error"] = "Geçersiz sipariş durumu.";
            return RedirectAfterStatusUpdate(returnTo, id, statusFilter, period, search);
        }

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id && o.BusinessId == businessId);

        if (order is null)
        {
            return NotFound();
        }

        var previousStatus = order.Status;
        var isTransitionToCompleted = status == OrderStatus.Completed && previousStatus != OrderStatus.Completed;

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        string? loyaltyMessage = null;

        if (isTransitionToCompleted)
        {
            var awardResult = await TryAwardCompletionPointsAsync(order, businessId);
            loyaltyMessage = awardResult.Message;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = loyaltyMessage ?? $"Sipariş durumu \"{OrderDisplayHelper.GetStatusLabel(status)}\" olarak güncellendi.";

        var statusChangeSummary = isTransitionToCompleted
            ? $"Sipariş durumu güncellendi: {previousStatus} → {status} (sadakat puanı akışı tetiklendi)."
            : $"Sipariş durumu güncellendi: {previousStatus} → {status}.";

        await _auditLog.LogBusinessAsync(
            businessId,
            "Order.StatusChanged",
            "Order",
            order.Id,
            statusChangeSummary,
            new
            {
                orderId = order.Id,
                oldStatus = previousStatus.ToString(),
                newStatus = status.ToString(),
                totalAmount = order.TotalAmount
            });

        return RedirectAfterStatusUpdate(returnTo, id, statusFilter, period, search);
    }

    private IActionResult RedirectAfterStatusUpdate(
        string? returnTo,
        int id,
        string? statusFilter,
        string? period,
        string? search)
    {
        if (string.Equals(returnTo, "list", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(nameof(Index), new
            {
                status = statusFilter,
                period,
                search
            });
        }

        if (string.Equals(returnTo, "kitchen", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(nameof(Kitchen));
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private static async Task<List<KitchenOrderCardViewModel>> MapKitchenOrdersAsync(
        IQueryable<Order> query)
    {
        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new KitchenOrderCardViewModel
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.CustomerName,
                CustomerPhone = o.CustomerPhone,
                CreatedAt = o.CreatedAt,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                Notes = o.Notes,
                Items = o.Items
                    .OrderBy(i => i.Id)
                    .Select(i => new OrderItemViewModel
                    {
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    private async Task ApplyLoyaltyInfoAsync(OrderDetailsViewModel viewModel, Order order, int businessId)
    {
        var customer = await ResolveCustomerAsync(order, businessId);
        viewModel.HasMatchingCustomer = customer is not null;

        var activeRule = await GetActiveLoyaltyRuleAsync(businessId);
        viewModel.HasActiveLoyaltyRule = activeRule is not null;

        if (activeRule is not null && customer is not null)
        {
            var potentialPoints = OrderLoyaltyHelper.CalculateEarnedPoints(order.TotalAmount, activeRule.PointsPerAmount);
            viewModel.PotentialLoyaltyPoints = potentialPoints > 0 ? potentialPoints : null;

            var description = OrderLoyaltyHelper.BuildCompletionDescription(order.OrderNumber);
            var awardedPoints = await _context.LoyaltyTransactions
                .AsNoTracking()
                .Where(t =>
                    t.BusinessId == businessId &&
                    t.CustomerId == customer.Id &&
                    t.Type == LoyaltyTransactionType.Earn &&
                    t.Description == description)
                .Select(t => (int?)t.Points)
                .FirstOrDefaultAsync();

            viewModel.AwardedLoyaltyPoints = awardedPoints;
        }
    }

    private async Task<LoyaltyAwardResult> TryAwardCompletionPointsAsync(Order order, int businessId)
    {
        var customer = await ResolveCustomerAsync(order, businessId);
        if (customer is null)
        {
            return LoyaltyAwardResult.NoCustomer();
        }

        var activeRule = await GetActiveLoyaltyRuleAsync(businessId);
        if (activeRule is null || activeRule.PointsPerAmount <= 0)
        {
            return LoyaltyAwardResult.NoAward();
        }

        var earnedPoints = OrderLoyaltyHelper.CalculateEarnedPoints(order.TotalAmount, activeRule.PointsPerAmount);
        if (earnedPoints <= 0)
        {
            return LoyaltyAwardResult.NoAward();
        }

        var description = OrderLoyaltyHelper.BuildCompletionDescription(order.OrderNumber);
        var alreadyAwarded = await _context.LoyaltyTransactions.AnyAsync(t =>
            t.BusinessId == businessId &&
            t.CustomerId == customer.Id &&
            t.Type == LoyaltyTransactionType.Earn &&
            t.Description == description);

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

    private async Task<Customer?> ResolveCustomerAsync(Order order, int businessId)
    {
        if (order.CustomerId.HasValue)
        {
            var customerById = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == order.CustomerId.Value && c.BusinessId == businessId);

            if (customerById is not null)
            {
                return customerById;
            }
        }

        if (string.IsNullOrWhiteSpace(order.CustomerPhone))
        {
            return null;
        }

        var normalizedPhone = order.CustomerPhone.Trim();
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.BusinessId == businessId && c.Phone == normalizedPhone);
    }

    private async Task<LoyaltyRule?> GetActiveLoyaltyRuleAsync(int businessId)
    {
        return await _context.LoyaltyRules
            .Where(r => r.BusinessId == businessId && r.IsActive && r.PointsPerAmount > 0)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();
    }

    private sealed class LoyaltyAwardResult
    {
        public string? Message { get; init; }

        public static LoyaltyAwardResult Awarded(int points) => new()
        {
            Message = $"Sipariş tamamlandı ve müşteriye {points} puan eklendi."
        };

        public static LoyaltyAwardResult NoCustomer() => new()
        {
            Message = "Sipariş tamamlandı. Müşteri kaydı bulunamadığı için puan eklenmedi."
        };

        public static LoyaltyAwardResult NoAward() => new();
    }
}
