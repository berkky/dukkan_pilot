using DukkanPilot.Core.Common;

namespace DukkanPilot.Core.Entities;

public class Category : BaseEntity
{
    public int BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }

    public Business Business { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
