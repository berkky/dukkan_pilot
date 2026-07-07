namespace DukkanPilot.Web.Areas.Business.Models;

public class CustomersIndexViewModel
{
    public int TotalCustomers { get; set; }

    public int NewCustomersThisMonth { get; set; }

    public int ReturningCustomers { get; set; }

    public int VipCustomers { get; set; }

    public int AtRiskCustomers { get; set; }

    public decimal TotalCustomerRevenue { get; set; }

    public decimal AverageCustomerSpend { get; set; }

    public string? Search { get; set; }

    public string SegmentFilter { get; set; } = "all";

    public string LastOrderFilter { get; set; } = "all";

    public decimal? MinSpend { get; set; }

    public decimal? MaxSpend { get; set; }

    public string ExportCsvUrl { get; set; } = "/Business/Customers/ExportCsv";

    public List<CustomerRowViewModel> Customers { get; set; } = [];
}

public class CustomerRowViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public int TotalPoints { get; set; }

    public bool IsActive { get; set; }

    public int OrderCount { get; set; }

    public decimal TotalSpent { get; set; }

    public decimal AverageBasket { get; set; }

    public DateTime? LastOrderDate { get; set; }

    public string Segment { get; set; } = string.Empty;

    public string SegmentLabel { get; set; } = string.Empty;

    public string SegmentBadgeClass { get; set; } = string.Empty;

    public string? WhatsAppContactUrl { get; set; }
}

public class CustomerTopProductViewModel
{
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal TotalSpent { get; set; }
}

public class CustomerInsightsViewModel
{
    public List<CustomerSegmentSummaryViewModel> SegmentDistribution { get; set; } = [];

    public List<CustomerRowViewModel> TopValuableCustomers { get; set; } = [];

    public List<CustomerRowViewModel> TopFrequentCustomers { get; set; } = [];

    public List<CustomerRowViewModel> AtRiskCustomers { get; set; } = [];

    public List<CustomerRowViewModel> NewCustomers { get; set; } = [];
}

public class CustomerSegmentSummaryViewModel
{
    public string Segment { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public int Count { get; set; }

    public int Percent { get; set; }

    public string BadgeClass { get; set; } = string.Empty;
}
