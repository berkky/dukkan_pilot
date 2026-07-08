using DukkanPilot.Web.Services;

namespace DukkanPilot.Web.Areas.Admin.Models;

public class AdminSalesRequestListViewModel
{
    public AdminSalesRequestSummary Summary { get; set; } = new();

    public string? Status { get; set; }

    public string? RequestType { get; set; }

    public string? Source { get; set; }

    public string? Priority { get; set; }

    public string? Search { get; set; }

    public int? PlanId { get; set; }

    public List<AdminSalesRequestRowViewModel> Items { get; set; } = [];
}

public class AdminSalesRequestRowViewModel
{
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string Source { get; set; } = string.Empty;

    public string RequestType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public string? ContactName { get; set; }

    public string? BusinessName { get; set; }

    public string? Email { get; set; }

    public string? RequestedPlanName { get; set; }

    public int? BusinessId { get; set; }

    public int? RequestedPlanId { get; set; }
}
