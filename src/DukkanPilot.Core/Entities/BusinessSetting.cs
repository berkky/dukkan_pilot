using DukkanPilot.Core.Common;

namespace DukkanPilot.Core.Entities;

public class BusinessSetting : BaseEntity
{
    public int BusinessId { get; set; }
    public string? WhatsAppNumber { get; set; }
    public string ThemeColor { get; set; } = "#2563eb";
    public string Currency { get; set; } = "TRY";

    public Business Business { get; set; } = null!;
}
