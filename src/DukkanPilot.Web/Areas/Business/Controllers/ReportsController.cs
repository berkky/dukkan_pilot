using System.Globalization;
using System.Text;
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
    public async Task<IActionResult> Index(string? period, DateTime? startDate, DateTime? endDate)
    {
        ViewData["ActiveMenu"] = "reports";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var model = await BuildReportsIndexAsync(businessId, period, startDate, endDate);
        return View(model);
    }

    [HttpGet("ExportCsv")]
    public async Task<IActionResult> ExportCsv(string? period, DateTime? startDate, DateTime? endDate)
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var range = ReportPeriodHelper.Resolve(period, startDate, endDate);
        var culture = CultureInfo.GetCultureInfo("tr-TR");

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.BusinessId == businessId
                && o.CreatedAt >= range.StartUtc
                && o.CreatedAt < range.EndUtc)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.OrderNumber,
                o.CreatedAt,
                o.CustomerName,
                o.CustomerPhone,
                o.Status,
                o.TotalAmount
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Sipariş No,Tarih,Müşteri,Telefon,Durum,Toplam Tutar");

        foreach (var order in orders)
        {
            sb.Append(CsvEscape(order.OrderNumber));
            sb.Append(',');
            sb.Append(CsvEscape(order.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm", culture)));
            sb.Append(',');
            sb.Append(CsvEscape(order.CustomerName));
            sb.Append(',');
            sb.Append(CsvEscape(order.CustomerPhone));
            sb.Append(',');
            sb.Append(CsvEscape(OrderDisplayHelper.GetStatusLabel(order.Status)));
            sb.Append(',');
            sb.Append(CsvEscape(order.TotalAmount.ToString("N2", culture)));
            sb.AppendLine();
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var fileName = $"dukkanpilot-rapor-{DateTime.Now:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
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

    private async Task<ReportsIndexViewModel> BuildReportsIndexAsync(
        int businessId,
        string? period,
        DateTime? startDate,
        DateTime? endDate)
    {
        var range = ReportPeriodHelper.Resolve(period, startDate, endDate);
        var culture = CultureInfo.GetCultureInfo("tr-TR");

        var ordersInRange = _context.Orders.AsNoTracking()
            .Where(o => o.BusinessId == businessId
                && o.CreatedAt >= range.StartUtc
                && o.CreatedAt < range.EndUtc);

        var revenueOrders = ordersInRange.Where(o => o.Status != OrderStatus.Cancelled);
        var hasRevenueOrders = await revenueOrders.AnyAsync();

        var kpis = new ReportKpiViewModel
        {
            TotalRevenue = await revenueOrders.SumAsync(o => o.TotalAmount),
            TotalOrders = await ordersInRange.CountAsync(),
            CompletedOrders = await ordersInRange.CountAsync(o => o.Status == OrderStatus.Completed),
            PendingOrders = await ordersInRange.CountAsync(o => o.Status == OrderStatus.Pending),
            PreparingOrders = await ordersInRange.CountAsync(o => o.Status == OrderStatus.Preparing),
            CancelledOrders = await ordersInRange.CountAsync(o => o.Status == OrderStatus.Cancelled),
            AverageBasket = hasRevenueOrders ? await revenueOrders.AverageAsync(o => o.TotalAmount) : 0,
            MaxOrderAmount = hasRevenueOrders ? await revenueOrders.MaxAsync(o => o.TotalAmount) : 0,
            MinOrderAmount = hasRevenueOrders ? await revenueOrders.MinAsync(o => o.TotalAmount) : 0
        };

        var orderSnapshots = await ordersInRange
            .Select(o => new { o.CreatedAt, o.TotalAmount, o.Status })
            .ToListAsync();

        var dailyPerformance = BuildDailyPerformance(
            orderSnapshots.Select(o => (o.CreatedAt, o.TotalAmount, o.Status)),
            range,
            culture);
        var maxDayRevenue = dailyPerformance.Count > 0 ? dailyPerformance.Max(d => d.Revenue) : 0m;

        if (maxDayRevenue > 0)
        {
            foreach (var day in dailyPerformance)
            {
                day.RevenueBarPercent = (int)Math.Round(day.Revenue * 100m / maxDayRevenue);
            }
        }

        var topProducts = await _context.OrderItems
            .AsNoTracking()
            .Where(i => i.Order.BusinessId == businessId
                && i.Order.Status != OrderStatus.Cancelled
                && i.Order.CreatedAt >= range.StartUtc
                && i.Order.CreatedAt < range.EndUtc)
            .GroupBy(i => i.ProductName)
            .Select(g => new ReportTopProductViewModel
            {
                ProductName = g.Key,
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.UnitPrice * i.Quantity),
                OrderCount = g.Select(i => i.OrderId).Distinct().Count()
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(10)
            .ToListAsync();

        var recentOrders = await ordersInRange
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => new SalesReportOrderRowViewModel
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                CreatedAt = o.CreatedAt,
                CustomerName = o.CustomerName,
                TotalAmount = o.TotalAmount,
                Status = o.Status
            })
            .ToListAsync();

        return new ReportsIndexViewModel
        {
            Period = range.Period,
            PeriodLabel = range.PeriodLabel,
            StartDateLocal = range.StartLocal,
            EndDateLocal = range.EndLocal,
            WasDateRangeAdjusted = range.WasDateRangeAdjusted,
            Kpis = kpis,
            DailyPerformance = dailyPerformance,
            TopProducts = topProducts,
            StatusDistribution = new DashboardStatusDistributionViewModel
            {
                PendingCount = kpis.PendingOrders,
                PreparingCount = kpis.PreparingOrders,
                CompletedCount = kpis.CompletedOrders,
                CancelledCount = kpis.CancelledOrders
            },
            RecentOrders = recentOrders,
            CampaignImpact = await BuildCampaignImpactAsync(businessId, range.StartUtc, range.EndUtc)
        };
    }

    [HttpGet("Campaigns")]
    public async Task<IActionResult> Campaigns(string? period, DateTime? startDate, DateTime? endDate)
    {
        ViewData["ActiveMenu"] = "reports-campaigns";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var range = ReportPeriodHelper.Resolve(period, startDate, endDate);
        var impact = await BuildCampaignImpactAsync(businessId, range.StartUtc, range.EndUtc);

        var recentCampaignOrders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.BusinessId == businessId
                && o.CreatedAt >= range.StartUtc
                && o.CreatedAt < range.EndUtc
                && o.Status != OrderStatus.Cancelled
                && o.DiscountAmount > 0)
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
            .ToListAsync();

        var model = new CampaignReportIndexViewModel
        {
            Period = range.Period,
            PeriodLabel = range.PeriodLabel,
            StartDateLocal = range.StartLocal,
            EndDateLocal = range.EndLocal,
            WasDateRangeAdjusted = range.WasDateRangeAdjusted,
            Impact = impact,
            RecentCampaignOrders = recentCampaignOrders
        };

        return View(model);
    }

    [HttpGet("CampaignsExportCsv")]
    public async Task<IActionResult> CampaignsExportCsv(string? period, DateTime? startDate, DateTime? endDate)
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var range = ReportPeriodHelper.Resolve(period, startDate, endDate);
        var impact = await BuildCampaignImpactAsync(businessId, range.StartUtc, range.EndUtc);
        var culture = CultureInfo.GetCultureInfo("tr-TR");

        var sb = new StringBuilder();
        sb.AppendLine("Kampanya,Sipariş Sayısı,Ara Toplam,Toplam İndirim,Net Ciro,Ortalama Sepet,Son Kullanım");

        foreach (var row in impact.TopCampaigns)
        {
            sb.Append(CsvEscape(row.CampaignName));
            sb.Append(',');
            sb.Append(row.OrderCount);
            sb.Append(',');
            sb.Append(CsvEscape(row.TotalSubtotal.ToString("N2", culture)));
            sb.Append(',');
            sb.Append(CsvEscape(row.TotalDiscount.ToString("N2", culture)));
            sb.Append(',');
            sb.Append(CsvEscape(row.NetRevenue.ToString("N2", culture)));
            sb.Append(',');
            sb.Append(CsvEscape(row.AverageBasket.ToString("N2", culture)));
            sb.Append(',');
            sb.Append(CsvEscape(row.LastUsedAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm", culture)));
            sb.AppendLine();
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var fileName = $"dukkanpilot-kampanya-rapor-{DateTime.Now:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    private async Task<ReportCampaignImpactViewModel> BuildCampaignImpactAsync(
        int businessId,
        DateTime startUtc,
        DateTime endUtc)
    {
        var revenueOrders = _context.Orders.AsNoTracking()
            .Where(o => o.BusinessId == businessId
                && o.CreatedAt >= startUtc
                && o.CreatedAt < endUtc
                && o.Status != OrderStatus.Cancelled);

        var withCampaign = revenueOrders.Where(o => o.DiscountAmount > 0);
        var withoutCampaign = revenueOrders.Where(o => o.DiscountAmount <= 0);

        var ordersWithCampaign = await withCampaign.CountAsync();
        var ordersWithoutCampaign = await withoutCampaign.CountAsync();
        var totalDiscount = await withCampaign.SumAsync(o => (decimal?)o.DiscountAmount) ?? 0m;
        var campaignOrderRevenue = await withCampaign.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
        var campaignOrderSubtotal = await withCampaign.SumAsync(o => (decimal?)o.SubtotalAmount) ?? 0m;
        var averageCampaignBasket = ordersWithCampaign > 0 ? campaignOrderRevenue / ordersWithCampaign : 0m;
        var discountRatePercent = campaignOrderSubtotal > 0
            ? Math.Round(totalDiscount * 100m / campaignOrderSubtotal, 2)
            : 0m;

        var topCampaigns = await withCampaign
            .GroupBy(o => new
            {
                o.AppliedCampaignId,
                Name = o.AppliedCampaignName ?? "Bilinmeyen Kampanya"
            })
            .Select(g => new ReportCampaignPerformanceRowViewModel
            {
                CampaignId = g.Key.AppliedCampaignId,
                CampaignName = g.Key.Name,
                OrderCount = g.Count(),
                TotalSubtotal = g.Sum(o => o.SubtotalAmount),
                TotalDiscount = g.Sum(o => o.DiscountAmount),
                NetRevenue = g.Sum(o => o.TotalAmount),
                AverageBasket = g.Average(o => o.TotalAmount),
                AverageDiscount = g.Average(o => o.DiscountAmount),
                LastUsedAt = g.Max(o => (DateTime?)o.CreatedAt)
            })
            .OrderByDescending(c => c.TotalDiscount)
            .ThenByDescending(c => c.OrderCount)
            .Take(10)
            .ToListAsync();

        return new ReportCampaignImpactViewModel
        {
            OrdersWithCampaign = ordersWithCampaign,
            OrdersWithoutCampaign = ordersWithoutCampaign,
            TotalDiscount = totalDiscount,
            CampaignOrderRevenue = campaignOrderRevenue,
            CampaignOrderSubtotal = campaignOrderSubtotal,
            AverageCampaignBasket = averageCampaignBasket,
            DiscountRatePercent = discountRatePercent,
            TopCampaigns = topCampaigns
        };
    }

    private static List<ReportDailyRevenueViewModel> BuildDailyPerformance(
        IEnumerable<(DateTime CreatedAt, decimal TotalAmount, OrderStatus Status)> orderSnapshots,
        ReportPeriodRange range,
        CultureInfo culture)
    {
        var snapshots = orderSnapshots
            .Select(o => (LocalDate: o.CreatedAt.ToLocalTime().Date, o.TotalAmount, o.Status))
            .ToList();

        var dailyPerformance = new List<ReportDailyRevenueViewModel>();

        for (var day = range.StartLocal; day <= range.EndLocal; day = day.AddDays(1))
        {
            var dayOrders = snapshots.Where(o => o.LocalDate == day).ToList();
            var revenueOrders = dayOrders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
            var revenue = revenueOrders.Sum(o => o.TotalAmount);

            dailyPerformance.Add(new ReportDailyRevenueViewModel
            {
                Date = day,
                DateLabel = day.ToString("dd.MM.yyyy", culture),
                OrderCount = dayOrders.Count,
                Revenue = revenue,
                AverageBasket = revenueOrders.Count > 0 ? revenue / revenueOrders.Count : 0
            });
        }

        return dailyPerformance;
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

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
