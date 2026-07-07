namespace DukkanPilot.Web.Areas.Business.Models;

public class CategoryListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
    public int ActiveProductCount { get; set; }
    public decimal AveragePrice { get; set; }
    public bool IsPublicVisible { get; set; }
}
