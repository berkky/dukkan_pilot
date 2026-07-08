namespace DukkanPilot.Web.Areas.Business.Models;

public class BusinessSalesRequestListViewModel
{
    public List<BusinessSalesRequestRowViewModel> Items { get; set; } = [];
}

public class BusinessSalesRequestRowViewModel
{
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? ClosedAtUtc { get; set; }

    public string RequestType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public string? CurrentPlanName { get; set; }

    public string? RequestedPlanName { get; set; }

    public string? Message { get; set; }
}
