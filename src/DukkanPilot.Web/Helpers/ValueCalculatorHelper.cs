using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Core.Enums;
using DukkanPilot.Web.Models.Value;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Helpers;

public static class ValueCalculatorHelper
{
    public const string ScenarioConservative = "Conservative";
    public const string ScenarioBase = "Base";
    public const string ScenarioAmbitious = "Ambitious";

    private const decimal WeeksPerMonth = 4.33m;

    private static readonly IReadOnlyDictionary<string, (string Label, decimal Multiplier)> ScenarioDefinitions =
        new Dictionary<string, (string, decimal)>(StringComparer.OrdinalIgnoreCase)
        {
            [ScenarioConservative] = ("Temkinli", 0.5m),
            [ScenarioBase] = ("Temel", 1.0m),
            [ScenarioAmbitious] = ("İddialı", 1.5m)
        };

    public static ValueCalculatorInputViewModel CreatePublicDefaults(decimal? monthlySoftwareCost = null) =>
        new()
        {
            MonthlyOrders = 300,
            AverageBasket = 250,
            RepeatCustomerRatePercent = 20,
            ExpectedRepeatIncreasePercent = 5,
            CampaignOrderIncreasePercent = 3,
            WeeklyMenuUpdateHours = 2,
            WeeklyOrderHandlingSavedHours = 3,
            HourlyLaborCost = 150,
            MonthlySoftwareCost = monthlySoftwareCost ?? 0,
            Scenario = ScenarioBase
        };

    public static ValueCalculatorInputViewModel SanitizeInput(ValueCalculatorInputViewModel input)
    {
        input.MonthlyOrders = ClampInt(input.MonthlyOrders, 0, 100_000);
        input.AverageBasket = ClampDecimal(input.AverageBasket, 0, 1_000_000);
        input.RepeatCustomerRatePercent = ClampDecimal(input.RepeatCustomerRatePercent, 0, 100);
        input.ExpectedRepeatIncreasePercent = ClampDecimal(input.ExpectedRepeatIncreasePercent, 0, 100);
        input.CampaignOrderIncreasePercent = ClampDecimal(input.CampaignOrderIncreasePercent, 0, 100);
        input.CurrentMarketplaceCommissionPercent = ClampDecimal(input.CurrentMarketplaceCommissionPercent, 0, 100);
        input.MarketplaceOrdersPercent = ClampDecimal(input.MarketplaceOrdersPercent, 0, 100);
        input.WeeklyMenuUpdateHours = ClampDecimal(input.WeeklyMenuUpdateHours, 0, 168);
        input.WeeklyOrderHandlingSavedHours = ClampDecimal(input.WeeklyOrderHandlingSavedHours, 0, 168);
        input.HourlyLaborCost = ClampDecimal(input.HourlyLaborCost, 0, 10_000);
        input.MonthlySoftwareCost = ClampDecimal(input.MonthlySoftwareCost, 0, 1_000_000);

        if (!ScenarioDefinitions.ContainsKey(input.Scenario))
        {
            input.Scenario = ScenarioBase;
        }

        return input;
    }

    public static ValueCalculatorResultViewModel Calculate(
        ValueCalculatorInputViewModel input,
        string context = "Public")
    {
        var sanitized = SanitizeInput(input);
        var scenarioResults = ScenarioDefinitions
            .Select(kvp => BuildScenarioResult(sanitized, kvp.Key, kvp.Value.Label, kvp.Value.Multiplier))
            .ToList();

        var selected = scenarioResults.FirstOrDefault(s =>
                             string.Equals(s.ScenarioKey, sanitized.Scenario, StringComparison.OrdinalIgnoreCase))
                         ?? scenarioResults.First(s => s.ScenarioKey == ScenarioBase);

        return new ValueCalculatorResultViewModel
        {
            Input = sanitized,
            ScenarioResults = scenarioResults,
            SelectedScenario = selected,
            MonthlyIncrementalRevenueEstimate = selected.IncrementalRevenueEstimate,
            MonthlyCommissionSavingEstimate = selected.CommissionSavingEstimate,
            MonthlyTimeSavingEstimate = selected.TimeSavingEstimate,
            MonthlyTotalEstimatedValue = selected.TotalEstimatedValue,
            YearlyTotalEstimatedValue = selected.TotalEstimatedValue * 12,
            NetMonthlyValue = selected.NetEstimatedValue,
            PaybackRatio = selected.PaybackRatio,
            BreakEvenText = selected.BreakEvenText,
            Warnings = BuildWarnings(),
            Assumptions = BuildAssumptions(sanitized),
            RecommendedNextSteps = BuildNextSteps(context)
        };
    }

    public static async Task<ValueCalculatorPrefillViewModel> BuildBusinessPrefillAsync(
        AppDbContext context,
        int businessId,
        CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-30);

        var orders = await context.Orders
            .AsNoTracking()
            .Where(o => o.BusinessId == businessId && o.CreatedAt >= since)
            .Where(o => o.Status != OrderStatus.Cancelled)
            .Select(o => new
            {
                o.TotalAmount,
                o.CustomerId,
                o.AppliedCampaignId
            })
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
        {
            return new ValueCalculatorPrefillViewModel
            {
                HasData = false,
                Note = "Son 30 günde sipariş bulunamadı; örnek varsayımlar kullanılıyor."
            };
        }

        var monthlyOrders = orders.Count;
        var averageBasket = orders.Average(o => o.TotalAmount);
        var customerIds = orders.Where(o => o.CustomerId.HasValue).Select(o => o.CustomerId!.Value).ToList();
        decimal? repeatRate = null;

        if (customerIds.Count > 0)
        {
            var repeatCustomers = customerIds.GroupBy(id => id).Count(g => g.Count() > 1);
            repeatRate = Math.Round((decimal)repeatCustomers / customerIds.Count * 100, 1);
        }

        var hasCampaign = orders.Any(o => o.AppliedCampaignId.HasValue);

        return new ValueCalculatorPrefillViewModel
        {
            HasData = true,
            SuggestedMonthlyOrders = monthlyOrders,
            SuggestedAverageBasket = Math.Round(averageBasket, 2),
            SuggestedRepeatRatePercent = repeatRate,
            HasCampaignUsage = hasCampaign,
            Note = "Son 30 günlük sipariş verilerinizden önerilen başlangıç değerleri. Varsayımlarınızı değiştirebilirsiniz."
        };
    }

    public static ValueCalculatorInputViewModel ApplyPrefill(
        ValueCalculatorInputViewModel input,
        ValueCalculatorPrefillViewModel? prefill)
    {
        if (prefill is not { HasData: true })
        {
            return input;
        }

        if (prefill.SuggestedMonthlyOrders.HasValue)
        {
            input.MonthlyOrders = prefill.SuggestedMonthlyOrders.Value;
        }

        if (prefill.SuggestedAverageBasket.HasValue)
        {
            input.AverageBasket = prefill.SuggestedAverageBasket.Value;
        }

        if (prefill.SuggestedRepeatRatePercent.HasValue)
        {
            input.RepeatCustomerRatePercent = prefill.SuggestedRepeatRatePercent.Value;
        }

        return input;
    }

    private static ValueScenarioResultViewModel BuildScenarioResult(
        ValueCalculatorInputViewModel input,
        string key,
        string label,
        decimal multiplier)
    {
        var incrementalRevenue = input.MonthlyOrders * input.AverageBasket
            * ((input.ExpectedRepeatIncreasePercent + input.CampaignOrderIncreasePercent) / 100m)
            * multiplier;

        var commissionSaving = 0m;
        if (input.MarketplaceOrdersPercent > 0 && input.CurrentMarketplaceCommissionPercent > 0)
        {
            commissionSaving = input.MonthlyOrders
                * (input.MarketplaceOrdersPercent / 100m)
                * input.AverageBasket
                * (input.CurrentMarketplaceCommissionPercent / 100m)
                * multiplier;
        }

        var timeSaving = (input.WeeklyMenuUpdateHours + input.WeeklyOrderHandlingSavedHours)
            * WeeksPerMonth
            * input.HourlyLaborCost
            * multiplier;

        var total = incrementalRevenue + commissionSaving + timeSaving;
        var net = total - input.MonthlySoftwareCost;
        decimal? payback = input.MonthlySoftwareCost > 0 ? total / input.MonthlySoftwareCost : null;

        var breakEven = total >= input.MonthlySoftwareCost
            ? "Bu varsayımlarla yazılım maliyetini karşılayabilir."
            : "Bu varsayımlarla maliyeti karşılaması için ek satış veya zaman tasarrufu gerekir.";

        return new ValueScenarioResultViewModel
        {
            ScenarioKey = key,
            ScenarioLabel = label,
            Multiplier = multiplier,
            IncrementalRevenueEstimate = RoundMoney(incrementalRevenue),
            CommissionSavingEstimate = RoundMoney(commissionSaving),
            TimeSavingEstimate = RoundMoney(timeSaving),
            TotalEstimatedValue = RoundMoney(total),
            NetEstimatedValue = RoundMoney(net),
            PaybackRatio = payback.HasValue ? Math.Round(payback.Value, 2) : null,
            BreakEvenText = breakEven
        };
    }

    private static IReadOnlyList<string> BuildWarnings() =>
    [
        "Bu tahmini bir hesaplamadır; garanti gelir vaadi değildir.",
        "Gerçek sonuçlar işletme, fiyatlama, müşteri kitlesi ve operasyon kalitesine göre değişir.",
        "Komisyon tasarrufu yalnızca üçüncü taraf platform veya komisyon kullanan işletmeler için geçerlidir.",
        "Zaman tasarrufu işletmenin süreçlerine ve ekip alışkanlıklarına göre değişir.",
        "Bu hesaplama KDV, vergi, maliyet veya kâr marjı içermez.",
        "Resmi finansal danışmanlık değildir."
    ];

    private static IReadOnlyList<string> BuildAssumptions(ValueCalculatorInputViewModel input) =>
    [
        $"Aylık {input.MonthlyOrders} sipariş ve ortalama {input.AverageBasket:N0} ₺ sepet varsayılmıştır.",
        $"Tekrar sipariş artışı %{input.ExpectedRepeatIncreasePercent:N1}, kampanya etkisi %{input.CampaignOrderIncreasePercent:N1} olarak alınmıştır.",
        $"Haftalık menü güncelleme {input.WeeklyMenuUpdateHours:N1} saat, sipariş yönetimi tasarrufu {input.WeeklyOrderHandlingSavedHours:N1} saat kabul edilmiştir.",
        $"Aylık yazılım maliyeti {input.MonthlySoftwareCost:N0} ₺ olarak girilmiştir.",
        "Varsayımlarınızı değiştirerek farklı senaryoları karşılaştırabilirsiniz."
    ];

    private static IReadOnlyList<string> BuildNextSteps(string context) =>
        context switch
        {
            "Business" =>
            [
                "Kampanya oluşturarak tekrar sipariş senaryosunu test edin.",
                "Sadakat ödülü tanımlayarak müşteri geri dönüşünü artırın.",
                "QR menüyü paylaşarak doğrudan sipariş kanalını güçlendirin.",
                "Go-Live kontrol listesini tamamlayarak operasyonu hızlandırın."
            ],
            "Admin" =>
            [
                "Demo görüşmesinde temkinli senaryoyu önce gösterin.",
                "İşletmenin hedeflerine göre varsayımları birlikte güncelleyin.",
                "Plan talebi veya demo sonrası takip için SalesRequests kaydını kullanın.",
                "Help Center satış makalelerini görüşme notlarına ekleyin."
            ],
            _ =>
            [
                "Planları inceleyerek işletmenize uygun paketi seçin.",
                "Demo talep ederek ürün akışını canlı görün.",
                "Plan talebi oluşturarak satış ekibiyle iletişime geçin.",
                "Demo QR menüyü inceleyerek müşteri deneyimini test edin."
            ]
        };

    private static int ClampInt(int value, int min, int max) => Math.Clamp(value, min, max);

    private static decimal ClampDecimal(decimal value, decimal min, decimal max) =>
        value < min ? min : value > max ? max : value;

    private static decimal RoundMoney(decimal value) => Math.Round(value, 2);
}
