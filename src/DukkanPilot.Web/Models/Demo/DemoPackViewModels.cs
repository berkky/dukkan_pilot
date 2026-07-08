using System.Collections.Generic;

namespace DukkanPilot.Web.Models.Demo;

public class DemoPackCardViewModel
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string VerticalName { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string BestFor { get; set; } = string.Empty;
    public string ScenarioSummary { get; set; } = string.Empty;

    public string PublicMenuUrl { get; set; } = string.Empty;
    public string RoiCalculatorUrl { get; set; } = string.Empty;
    public string SuggestedPlanName { get; set; } = string.Empty;

    public IReadOnlyList<string> KeyFeatures { get; set; } = new List<string>();
    public IReadOnlyList<string> DemoTalkingPoints { get; set; } = new List<string>();
    public IReadOnlyList<string> SampleCategories { get; set; } = new List<string>();
    public IReadOnlyList<string> SampleCampaigns { get; set; } = new List<string>();
    public IReadOnlyList<string> SampleRewards { get; set; } = new List<string>();

    public string? BadgeText { get; set; }
    public int SortOrder { get; set; }
}

public class DemoPacksPageViewModel
{
    public string PageTitle { get; set; } = "Demo Paketleri";
    public string Intro { get; set; } = string.Empty;
    public IReadOnlyList<DemoPackCardViewModel> Packs { get; set; } = new List<DemoPackCardViewModel>();
}
