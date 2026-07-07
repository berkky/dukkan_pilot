namespace DukkanPilot.Web.Models.PublicMenu;

public class PublicMenuCategoryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<PublicMenuProductViewModel> Products { get; set; } = new();
}
