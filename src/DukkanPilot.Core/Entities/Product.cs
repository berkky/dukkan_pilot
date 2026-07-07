using DukkanPilot.Core.Common;

namespace DukkanPilot.Core.Entities;

public class Product : BaseEntity
{
    public int BusinessId { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? SizeOption { get; set; }
    public int SortOrder { get; set; }

    public Business Business { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
