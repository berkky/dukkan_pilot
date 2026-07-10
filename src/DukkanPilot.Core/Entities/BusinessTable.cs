using DukkanPilot.Core.Common;

namespace DukkanPilot.Core.Entities;

public class BusinessTable : BaseEntity
{
    public int BusinessId { get; set; }
    public string TableLabel { get; set; } = string.Empty;
    public string PublicCode { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public Business Business { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
