using DukkanPilot.Core.Common;

namespace DukkanPilot.Core.Entities;

public class QrCode : BaseEntity
{
    public int BusinessId { get; set; }
    public string Label { get; set; } = "Ana Menü";
    public string TargetUrl { get; set; } = string.Empty;
    public DateTime? LastGeneratedAt { get; set; }

    public Business Business { get; set; } = null!;
}
