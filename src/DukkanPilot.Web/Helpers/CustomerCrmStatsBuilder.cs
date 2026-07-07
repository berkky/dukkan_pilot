using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Helpers;

public static class CustomerCrmStatsBuilder
{
    public static List<CustomerCrmStats> Build(IEnumerable<Customer> customers, IEnumerable<Order> orders, DateTime utcNow)
    {
        var orderList = orders.ToList();
        var ordersByCustomerId = orderList
            .Where(o => o.CustomerId.HasValue)
            .GroupBy(o => o.CustomerId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var ordersByPhone = orderList
            .Where(o => o.CustomerId == null && !string.IsNullOrWhiteSpace(o.CustomerPhone))
            .GroupBy(o => o.CustomerPhone!.Trim())
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

        var statsList = new List<CustomerCrmStats>();
        var last30DaysStart = utcNow.AddDays(-30);

        foreach (var customer in customers)
        {
            var matchedOrders = new List<Order>();

            if (ordersByCustomerId.TryGetValue(customer.Id, out var byIdOrders))
            {
                matchedOrders.AddRange(byIdOrders);
            }

            if (!string.IsNullOrWhiteSpace(customer.Phone) &&
                ordersByPhone.TryGetValue(customer.Phone.Trim(), out var byPhoneOrders))
            {
                matchedOrders.AddRange(byPhoneOrders.Where(o =>
                    o.CustomerId == null || o.CustomerId == customer.Id));
            }

            matchedOrders = matchedOrders
                .GroupBy(o => o.Id)
                .Select(g => g.First())
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            var revenueOrders = matchedOrders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
            var orderCount = matchedOrders.Count;
            var totalSpent = revenueOrders.Sum(o => o.TotalAmount);
            var lastOrderDate = matchedOrders.FirstOrDefault()?.CreatedAt;
            var firstOrderDate = matchedOrders.LastOrDefault()?.CreatedAt;
            var maxOrderAmount = revenueOrders.Count > 0 ? revenueOrders.Max(o => o.TotalAmount) : 0m;
            var last30DaysOrderCount = matchedOrders.Count(o => o.CreatedAt >= last30DaysStart);

            var stats = new CustomerCrmStats
            {
                Id = customer.Id,
                Name = customer.Name,
                Phone = customer.Phone,
                Email = customer.Email,
                TotalPoints = customer.TotalPoints,
                IsActive = customer.IsActive,
                CreatedAt = customer.CreatedAt,
                OrderCount = orderCount,
                TotalSpent = totalSpent,
                AverageBasket = revenueOrders.Count > 0 ? totalSpent / revenueOrders.Count : 0m,
                LastOrderDate = lastOrderDate,
                FirstOrderDate = firstOrderDate,
                MaxOrderAmount = maxOrderAmount,
                Last30DaysOrderCount = last30DaysOrderCount,
                WhatsAppContactUrl = CustomerCrmHelper.BuildWhatsAppContactUrl(customer.Phone)
            };

            stats.Segment = CustomerCrmHelper.DeterminePrimarySegment(stats, utcNow);
            statsList.Add(stats);
        }

        return statsList;
    }
}
