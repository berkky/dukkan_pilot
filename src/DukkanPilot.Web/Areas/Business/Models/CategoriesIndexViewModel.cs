namespace DukkanPilot.Web.Areas.Business.Models;

public class CategoriesIndexViewModel
{
    public int TotalCategories { get; set; }

    public int ActiveCategories { get; set; }

    public int PassiveCategories { get; set; }

    public List<CategoryListViewModel> Categories { get; set; } = [];
}
