using System.Globalization;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Reports")]
[RequireActiveSubscription]
public class ReportsController : BusinessBaseController
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "reports";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);
        var last7DaysStart = todayStart.AddDays(-6);

        var todayOrders = _context.Orders.AsNoTracking()
            .Where(o => o.BusinessId == businessId && o.CreatedAt >= todayStart && o.CreatedAt < todayEnd);

        var last7DaysOrders = _context.Orders.AsNoTracking()
            .Where(o => o.BusinessId == businessId && o.CreatedAt >= last7DaysStart);

        var model = new ReportsOverviewViewModel
        {
            TodayOrderCount = await todayOrders.CountAsync(),
            TodayRevenue = await todayOrders.SumAsync(o => o.TotalAmount),
            Last7DaysOrderCount = await last7DaysOrders.CountAsync(),
            Last7DaysRevenue = await last7DaysOrders.SumAsync(o => o.TotalAmount),
            TotalCustomerCount = await _context.Customers.CountAsync(c => c.BusinessId == businessId),
            TotalProductCount = await _context.Products.CountAsync(p => p.BusinessId == businessId),
            ActiveCampaignCount = await _context.Campaigns.CountAsync(c => c.BusinessId == businessId && c.IsActive),
            TotalEarnedPoints = await _context.LoyaltyTransactions
                .Where(t => t.BusinessId == businessId && t.Type == LoyaltyTransactionType.Earn)
                .SumAsync(t => t.Points)
        };

        return View(model);
    }

    [HttpGet("Sales")]
    public async Task<IActionResult> Sales()
    {
        ViewData["ActiveMenu"] = "reports-sales";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);
        var last7DaysStart = todayStart.AddDays(-6);
        var last30DaysStart = todayStart.AddDays(-29);

        var allOrders = _context.Orders.AsNoTracking().Where(o => o.BusinessId == businessId);

        var model = new SalesReportViewModel
        {
            TodayRevenue = await allOrders
                .Where(o => o.CreatedAt >= todayStart && o.CreatedAt < todayEnd)
                .SumAsync(o => o.TotalAmount),
            Last7DaysRevenue = await allOrders
                .Where(o => o.CreatedAt >= last7DaysStart)
                .SumAsync(o => o.TotalAmount),
            Last30DaysRevenue = await allOrders
                .Where(o => o.CreatedAt >= last30DaysStart)
                .SumAsync(o => o.TotalAmount),
            AverageOrderAmount = await allOrders.AnyAsync()
                ? await allOrders.AverageAsync(o => o.TotalAmount)
                : 0,
            CompletedOrderCount = await allOrders.CountAsync(o => o.Status == OrderStatus.Completed),
            PendingOrderCount = await allOrders.CountAsync(o => o.Status == OrderStatus.Pending),
            CancelledOrderCount = await allOrders.CountAsync(o => o.Status == OrderStatus.Cancelled),
            RecentOrders = await allOrders
                .OrderByDescending(o => o.CreatedAt)
                .Take(20)
                .Select(o => new SalesReportOrderRowViewModel
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    CreatedAt = o.CreatedAt,
                    CustomerName = o.CustomerName,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status
                })
                .ToListAsync(),
            DailyChart = await BuildDailyRevenueChartAsync(businessId, last7DaysStart)
        };

        return View(model);
    }

    [HttpGet("Products")]
    public async Task<IActionResult> Products()
    {
        ViewData["ActiveMenu"] = "reports-products";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var productSales = await _context.OrderItems
            .AsNoTracking()
            .Where(i => i.Order.BusinessId == businessId && i.Order.Status != OrderStatus.Cancelled)
            .GroupBy(i => new
            {
                i.ProductId,
                i.ProductName,
                CategoryName = i.Product.Category.Name
            })
            .Select(g => new ProductSalesRowViewModel
            {
                ProductName = g.Key.ProductName,
                CategoryName = g.Key.CategoryName,
                QuantitySold = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.UnitPrice * i.Quantity)
            })
            .OrderByDescending(p => p.QuantitySold)
            .ToListAsync();

        var topProducts = productSales.Take(10).ToList();
        var chartProducts = productSales.Take(5).ToList();

        var model = new ProductReportViewModel
        {
            TotalProductCount = await _context.Products.CountAsync(p => p.BusinessId == businessId),
            ActiveProductCount = await _context.Products.CountAsync(p => p.BusinessId == businessId && p.IsActive),
            TopSellingProductName = productSales.FirstOrDefault()?.ProductName,
            TopRevenueProductName = productSales
                .OrderByDescending(p => p.TotalRevenue)
                .FirstOrDefault()?.ProductName,
            TopProducts = topProducts,
            ChartProducts = chartProducts
        };

        return View(model);
    }

    [HttpGet("Customers")]
    public async Task<IActionResult> Customers()
    {
        ViewData["ActiveMenu"] = "reports-customers";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var customerRows = await _context.Customers
            .AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .Select(c => new CustomerReportRowViewModel
            {
                CustomerId = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                TotalPoints = c.TotalPoints,
                OrderCount = _context.Orders.Count(o =>
                    o.BusinessId == businessId &&
                    (o.CustomerId == c.Id ||
                     (o.CustomerId == null && o.CustomerPhone != null && c.Phone != null && o.CustomerPhone == c.Phone))),
                TotalSpending = _context.Orders
                    .Where(o =>
                        o.BusinessId == businessId &&
                        o.Status != OrderStatus.Cancelled &&
                        (o.CustomerId == c.Id ||
                         (o.CustomerId == null && o.CustomerPhone != null && c.Phone != null && o.CustomerPhone == c.Phone)))
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0
            })
            .ToListAsync();

        var topCustomers = customerRows
            .OrderByDescending(c => c.TotalSpending)
            .ThenByDescending(c => c.OrderCount)
            .Take(10)
            .ToList();

        var model = new CustomerReportViewModel
        {
            TotalCustomerCount = customerRows.Count,
            ActiveCustomerCount = await _context.Customers.CountAsync(c => c.BusinessId == businessId && c.IsActive),
            TopOrderCustomerName = customerRows
                .OrderByDescending(c => c.OrderCount)
                .FirstOrDefault()?.Name,
            TopSpendingCustomerName = customerRows
                .OrderByDescending(c => c.TotalSpending)
                .FirstOrDefault()?.Name,
            TopPointsCustomerName = customerRows
                .OrderByDescending(c => c.TotalPoints)
                .FirstOrDefault()?.Name,
            TopCustomers = topCustomers
        };

        return View(model);
    }

    private async Task<List<SalesDailyChartItemViewModel>> BuildDailyRevenueChartAsync(int businessId, DateTime startDate)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var revenueByDay = await _context.Orders
            .AsNoTracking()
            .Where(o => o.BusinessId == businessId && o.CreatedAt >= startDate)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
            .ToListAsync();

        var chart = new List<SalesDailyChartItemViewModel>();
        for (var day = startDate.Date; day <= DateTime.UtcNow.Date; day = day.AddDays(1))
        {
            var revenue = revenueByDay.FirstOrDefault(x => x.Date == day)?.Revenue ?? 0;
            chart.Add(new SalesDailyChartItemViewModel
            {
                Label = day.ToString("dd.MM", culture),
                Revenue = revenue
            });
        }

        return chart;
    }
}
