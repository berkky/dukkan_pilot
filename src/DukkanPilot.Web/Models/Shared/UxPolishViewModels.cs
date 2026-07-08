namespace DukkanPilot.Web.Models.Shared;

public class EmptyStateViewModel
{
    public string Icon { get; set; } = "📋";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? PrimaryActionText { get; set; }
    public string? PrimaryActionUrl { get; set; }
    public string? SecondaryActionText { get; set; }
    public string? SecondaryActionUrl { get; set; }
    public string? HelpText { get; set; }
    public string? PrimaryButtonClass { get; set; }
}

public class HelpCardViewModel
{
    public string Title { get; set; } = "Bu ekran ne işe yarar?";
    public string Description { get; set; } = string.Empty;
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
    public string Variant { get; set; } = "info";
}
