using DukkanPilot.Core.Common;

namespace DukkanPilot.Core.Entities;

public class Campaign : BaseEntity
{
    public int BusinessId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public string? ImageUrl { get; set; }

    public Business Business { get; set; } = null!;
}
