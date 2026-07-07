using DukkanPilot.Core.Enums;
using DukkanPilot.Web.Areas.Business.Models;

namespace DukkanPilot.Web.Areas.Admin.Models;

public class AdminBusinessDetailsViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? LogoUrl { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? WhatsAppNumber { get; set; }

    public string PublicMenuUrl => $"/m/{Slug}";

    public AdminBusinessHealthViewModel Health { get; set; } = new();

    public AdminBusinessSubscriptionSummaryViewModel Subscription { get; set; } = new();

    public BusinessPlanUsageViewModel PlanUsage { get; set; } = new();

    public AdminBusinessMenuReadinessViewModel Menu { get; set; } = new();

    public AdminBusinessOrderSummaryViewModel OrderSummary { get; set; } = new();

    public List<AdminBusinessRecentOrderViewModel> RecentOrders { get; set; } = [];

    public List<AdminBusinessTopProductViewModel> TopProducts { get; set; } = [];

    public AdminBusinessUserSummaryViewModel Users { get; set; } = new();
}

public class AdminBusinessHealthViewModel
{
    public int Score { get; set; }

    public string Label { get; set; } = string.Empty;

    public string BadgeClass { get; set; } = "bg-secondary";

    public List<AdminBusinessRiskItemViewModel> Risks { get; set; } = [];
}

public class AdminBusinessRiskItemViewModel
{
    public string Reason { get; set; } = string.Empty;

    public string BadgeClass { get; set; } = "bg-warning text-dark";
}

public class AdminBusinessSubscriptionSummaryViewModel
{
    public string PlanName { get; set; } = "-";

    public string StatusText { get; set; } = "-";

    public string StatusBadgeClass { get; set; } = "bg-secondary";

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? DaysRemaining { get; set; }

    public decimal PlanPrice { get; set; }

    public bool IsValid { get; set; }
}

public class AdminBusinessMenuReadinessViewModel
{
    public int TotalCategories { get; set; }

    public int ActiveCategories { get; set; }

    public int TotalProducts { get; set; }

    public int ActiveProducts { get; set; }

    public int PassiveProducts { get; set; }

    public decimal AverageProductPrice { get; set; }

    public string PublicMenuUrl { get; set; } = string.Empty;
}

public class AdminBusinessOrderSummaryViewModel
{
    public int TotalOrders { get; set; }

    public decimal TotalRevenue { get; set; }

    public int TodayOrders { get; set; }

    public int Last7DaysOrders { get; set; }

    public decimal ThisMonthRevenue { get; set; }

    public decimal AverageBasket { get; set; }

    public DateTime? LastOrderAt { get; set; }
}

public class AdminBusinessRecentOrderViewModel
{
    public string OrderNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string CustomerName { get; set; } = "-";

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; }

    public string StatusText { get; set; } = string.Empty;

    public string StatusBadgeClass { get; set; } = "bg-secondary";
}

public class AdminBusinessTopProductViewModel
{
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal Revenue { get; set; }
}

public class AdminBusinessUserSummaryViewModel
{
    public string? OwnerName { get; set; }

    public string? OwnerEmail { get; set; }

    public int StaffCount { get; set; }

    public int TotalRoleCount { get; set; }
}
