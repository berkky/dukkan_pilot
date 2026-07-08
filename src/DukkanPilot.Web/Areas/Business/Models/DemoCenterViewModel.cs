namespace DukkanPilot.Web.Areas.Business.Models;

public class DemoCenterViewModel
{
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessSlug { get; set; } = string.Empty;
    public string PublicMenuUrl { get; set; } = string.Empty;
    public int ReadinessScore { get; set; }
    public string ReadinessLabel { get; set; } = string.Empty;
    public string ReadinessBadgeClass { get; set; } = "bg-secondary";
    public List<DemoCenterCheckViewModel> Checks { get; set; } = new();
    public List<DemoCenterStepViewModel> Steps { get; set; } = new();
}

public class DemoCenterCheckViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsReady { get; set; }
}

public class DemoCenterStepViewModel
{
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActionText { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    public bool OpenInNewTab { get; set; }
    public bool IsReady { get; set; }
    public string StatusText => IsReady ? "Hazır" : "Eksik";
    public string StatusBadgeClass => IsReady ? "bg-success" : "bg-warning text-dark";
}
