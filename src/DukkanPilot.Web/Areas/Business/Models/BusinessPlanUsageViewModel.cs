namespace DukkanPilot.Web.Areas.Business.Models;

public class BusinessPlanUsageViewModel
{
    public string PlanName { get; set; } = string.Empty;

    public bool HasValidSubscription { get; set; }

    public PlanUsageMetricViewModel Products { get; set; } = new() { Name = "Ürünler" };

    public PlanUsageMetricViewModel Categories { get; set; } = new() { Name = "Kategoriler" };

    public PlanUsageMetricViewModel StaffUsers { get; set; } = new() { Name = "Personel" };

    public PlanUsageMetricViewModel Campaigns { get; set; } = new() { Name = "Kampanyalar" };

    public PlanUsageMetricViewModel Rewards { get; set; } = new() { Name = "Ödüller" };

    public PlanUsageMetricViewModel QrCodes { get; set; } = new() { Name = "QR Kodlar" };

    public IReadOnlyList<PlanUsageMetricViewModel> AllMetrics =>
    [
        Products,
        Categories,
        StaffUsers,
        Campaigns,
        Rewards,
        QrCodes
    ];
}
