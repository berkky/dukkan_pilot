using DukkanPilot.Web.Areas.Business.Models;

namespace DukkanPilot.Web.Areas.Admin.Models;

public class AdminDashboardViewModel
{
    public AdminPlatformKpiViewModel Platform { get; set; } = new();

    public AdminSubscriptionKpiViewModel Subscriptions { get; set; } = new();

    public List<AdminPlanDistributionViewModel> PlanDistribution { get; set; } = [];

    public List<AdminBusinessActivityViewModel> TopActiveBusinesses { get; set; } = [];

    public List<AdminRiskyBusinessViewModel> RiskyBusinesses { get; set; } = [];

    public List<AdminBusinessActivityViewModel> RecentBusinesses { get; set; } = [];

    public List<NotificationRowViewModel> CriticalNotifications { get; set; } = [];

    public int NewSalesRequestCount { get; set; }

    public int OpenSalesRequestCount { get; set; }
}

public class AdminPlatformKpiViewModel
{
    public int TotalBusinesses { get; set; }

    public int ActiveBusinesses { get; set; }

    public int PassiveBusinesses { get; set; }

    public int TotalUsers { get; set; }

    public int BusinessOwnersCount { get; set; }

    public int StaffUsersCount { get; set; }

    public int TotalOrders { get; set; }

    public decimal TotalRevenue { get; set; }

    public int TodayOrders { get; set; }

    public decimal TodayRevenue { get; set; }

    public int Last7DaysOrders { get; set; }

    public decimal Last7DaysRevenue { get; set; }

    public int ThisMonthOrders { get; set; }

    public decimal ThisMonthRevenue { get; set; }
}

public class AdminSubscriptionKpiViewModel
{
    public int TrialSubscriptions { get; set; }

    public int ActiveSubscriptions { get; set; }

    public int ExpiredSubscriptions { get; set; }

    public int CancelledSubscriptions { get; set; }

    public int ExpiringSoonSubscriptions { get; set; }

    public int BusinessesWithoutSubscription { get; set; }
}

public class AdminPlanDistributionViewModel
{
    public int PlanId { get; set; }

    public string PlanName { get; set; } = string.Empty;

    public int BusinessCount { get; set; }

    public int ActiveSubscriptionCount { get; set; }

    public decimal MonthlyPotentialRevenue { get; set; }
}

public class AdminBusinessActivityViewModel
{
    public int BusinessId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public int OrderCount { get; set; }

    public decimal Revenue { get; set; }

    public int ActiveProductsCount { get; set; }

    public DateTime? LastOrderAt { get; set; }

    public string CurrentPlanName { get; set; } = "-";

    public string SubscriptionStatus { get; set; } = "-";

    public string SubscriptionStatusBadgeClass { get; set; } = "bg-secondary";

    public DateTime CreatedAt { get; set; }

    public string? OwnerEmail { get; set; }
}

public class AdminRiskyBusinessViewModel
{
    public int BusinessId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string RiskReason { get; set; } = string.Empty;

    public string RiskBadgeClass { get; set; } = "bg-warning text-dark";

    public decimal TotalRevenue { get; set; }

    public DateTime? LastOrderAt { get; set; }

    public DateTime? SubscriptionEndDate { get; set; }
}

public class BusinessesIndexViewModel
{
    public int TotalBusinesses { get; set; }

    public int ActiveBusinesses { get; set; }

    public int PassiveBusinesses { get; set; }

    public int ActiveSubscriptionBusinesses { get; set; }

    public int ExpiredSubscriptionBusinesses { get; set; }

    public string? Search { get; set; }

    public string StatusFilter { get; set; } = "all";

    public string? SubscriptionFilter { get; set; }

    public int? PlanFilter { get; set; }

    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> AvailablePlans { get; set; } = [];

    public string ExportCsvUrl { get; set; } = string.Empty;

    public List<BusinessListViewModel> Businesses { get; set; } = [];
}

public class SubscriptionPlanListViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int MaxProducts { get; set; }

    public int MaxCampaigns { get; set; }

    public bool IsActive { get; set; }

    public int ActiveBusinessCount { get; set; }

    public bool HasActiveSubscriptions { get; set; }
}
