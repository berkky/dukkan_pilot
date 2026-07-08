using System.ComponentModel.DataAnnotations;

namespace DukkanPilot.Web.Models.Value;

public class ValueCalculatorInputViewModel
{
    [Range(0, 100_000)]
    public int MonthlyOrders { get; set; } = 300;

    [Range(0, 1_000_000)]
    public decimal AverageBasket { get; set; } = 250;

    [Range(0, 100)]
    public decimal RepeatCustomerRatePercent { get; set; } = 20;

    [Range(0, 100)]
    public decimal ExpectedRepeatIncreasePercent { get; set; } = 5;

    [Range(0, 100)]
    public decimal CampaignOrderIncreasePercent { get; set; } = 3;

    [Range(0, 100)]
    public decimal CurrentMarketplaceCommissionPercent { get; set; }

    [Range(0, 100)]
    public decimal MarketplaceOrdersPercent { get; set; }

    [Range(0, 168)]
    public decimal WeeklyMenuUpdateHours { get; set; } = 2;

    [Range(0, 168)]
    public decimal WeeklyOrderHandlingSavedHours { get; set; } = 3;

    [Range(0, 10_000)]
    public decimal HourlyLaborCost { get; set; } = 150;

    [Range(0, 1_000_000)]
    public decimal MonthlySoftwareCost { get; set; }

    public string Scenario { get; set; } = "Base";
}

public class ValueScenarioResultViewModel
{
    public string ScenarioKey { get; set; } = string.Empty;

    public string ScenarioLabel { get; set; } = string.Empty;

    public decimal Multiplier { get; set; }

    public decimal IncrementalRevenueEstimate { get; set; }

    public decimal CommissionSavingEstimate { get; set; }

    public decimal TimeSavingEstimate { get; set; }

    public decimal TotalEstimatedValue { get; set; }

    public decimal NetEstimatedValue { get; set; }

    public decimal? PaybackRatio { get; set; }

    public string BreakEvenText { get; set; } = string.Empty;
}

public class ValueCalculatorResultViewModel
{
    public ValueCalculatorInputViewModel Input { get; set; } = new();

    public IReadOnlyList<ValueScenarioResultViewModel> ScenarioResults { get; set; } = Array.Empty<ValueScenarioResultViewModel>();

    public ValueScenarioResultViewModel SelectedScenario { get; set; } = new();

    public decimal MonthlyIncrementalRevenueEstimate { get; set; }

    public decimal MonthlyCommissionSavingEstimate { get; set; }

    public decimal MonthlyTimeSavingEstimate { get; set; }

    public decimal MonthlyTotalEstimatedValue { get; set; }

    public decimal YearlyTotalEstimatedValue { get; set; }

    public decimal NetMonthlyValue { get; set; }

    public decimal? PaybackRatio { get; set; }

    public string BreakEvenText { get; set; } = string.Empty;

    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> Assumptions { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> RecommendedNextSteps { get; set; } = Array.Empty<string>();
}

public class ValueCalculatorPrefillViewModel
{
    public bool HasData { get; set; }

    public string? Note { get; set; }

    public int? SuggestedMonthlyOrders { get; set; }

    public decimal? SuggestedAverageBasket { get; set; }

    public decimal? SuggestedRepeatRatePercent { get; set; }

    public bool HasCampaignUsage { get; set; }
}

public class ValueCalculatorBusinessOptionViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class ValueCalculatorPageViewModel
{
    public string PageTitle { get; set; } = "Değer Hesaplayıcı";

    public string Intro { get; set; } = string.Empty;

    public string FormAction { get; set; } = string.Empty;

    public ValueCalculatorInputViewModel Input { get; set; } = new();

    public ValueCalculatorResultViewModel? Result { get; set; }

    public bool ShowResult { get; set; }

    public ValueCalculatorPrefillViewModel? Prefill { get; set; }

    public bool IsBusinessOwner { get; set; }

    public bool ShowOwnerCtas { get; set; }

    public int? SelectedBusinessId { get; set; }

    public string? SelectedBusinessName { get; set; }

    public IReadOnlyList<ValueCalculatorBusinessOptionViewModel> Businesses { get; set; } = Array.Empty<ValueCalculatorBusinessOptionViewModel>();

    public string Context { get; set; } = "Public";
}
