using DukkanPilot.Web.Areas.Business.Models;

namespace DukkanPilot.Web.Models.Landing;

public class LandingAuthCtaViewModel
{
    public bool IsAuthenticated { get; set; }
    public string PrimaryText { get; set; } = "Ücretsiz Başla";
    public string PrimaryUrl { get; set; } = "/Account/Register";
    public string SecondaryText { get; set; } = "Giriş Yap";
    public string SecondaryUrl { get; set; } = "/Account/Login";
}

public class LandingPageViewModel
{
    public LandingAuthCtaViewModel AuthCta { get; set; } = new();
    public string DemoMenuUrl { get; set; } = "/m/demo-kafe";
    public List<LandingPlanCardViewModel> Plans { get; set; } = new();
    public bool PlansFromDatabase { get; set; }
}

public class PricingPageViewModel
{
    public LandingAuthCtaViewModel AuthCta { get; set; } = new();
    public List<LandingPlanCardViewModel> Plans { get; set; } = new();
    public bool PlansFromDatabase { get; set; }
}

public class LandingPlanCardViewModel
{
    public int? PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string ProductLimitText { get; set; } = string.Empty;
    public string CategoryLimitText { get; set; } = string.Empty;
    public string StaffLimitText { get; set; } = string.Empty;
    public string CampaignLimitText { get; set; } = string.Empty;
    public string RewardLimitText { get; set; } = string.Empty;
    public string QrCodeLimitText { get; set; } = string.Empty;
}

public class DemoPageViewModel
{
    public LandingAuthCtaViewModel AuthCta { get; set; } = new();
    public string DemoBusinessName { get; set; } = "Demo Kafe";
    public string DemoSlug { get; set; } = "demo-kafe";
    public string DemoMenuUrl { get; set; } = "/m/demo-kafe";
}

public static class LandingPlanMapper
{
    public static LandingPlanCardViewModel FromAvailablePlan(AvailablePlanViewModel plan)
    {
        return new LandingPlanCardViewModel
        {
            PlanId = plan.PlanId,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            ProductLimitText = plan.ProductLimitText,
            CategoryLimitText = plan.CategoryLimitText,
            StaffLimitText = plan.StaffLimitText,
            CampaignLimitText = plan.CampaignLimitText,
            RewardLimitText = plan.RewardLimitText,
            QrCodeLimitText = plan.QrCodeLimitText
        };
    }

    public static List<LandingPlanCardViewModel> FallbackPlans() =>
    [
        new()
        {
            Name = "Free",
            Description = "Başlangıç için temel QR menü ve sipariş deneyimi.",
            Price = 0,
            ProductLimitText = "20",
            CategoryLimitText = "5",
            StaffLimitText = "1",
            CampaignLimitText = "1",
            RewardLimitText = "2",
            QrCodeLimitText = "1"
        },
        new()
        {
            Name = "Starter",
            Description = "Büyüyen işletmeler için daha yüksek limitler.",
            Price = 499,
            ProductLimitText = "100",
            CategoryLimitText = "15",
            StaffLimitText = "3",
            CampaignLimitText = "5",
            RewardLimitText = "10",
            QrCodeLimitText = "5"
        },
        new()
        {
            Name = "Pro",
            Description = "Kampanya, sadakat ve operasyon için geniş limitler.",
            Price = 999,
            ProductLimitText = "500",
            CategoryLimitText = "50",
            StaffLimitText = "10",
            CampaignLimitText = "25",
            RewardLimitText = "50",
            QrCodeLimitText = "20"
        }
    ];
}
