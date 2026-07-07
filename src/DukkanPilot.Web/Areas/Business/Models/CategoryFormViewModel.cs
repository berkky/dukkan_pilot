using System.ComponentModel.DataAnnotations;

namespace DukkanPilot.Web.Areas.Business.Models;

public class CategoryFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Kategori adı zorunludur.")]
    [MaxLength(150, ErrorMessage = "Kategori adı en fazla 150 karakter olabilir.")]
    [Display(Name = "Kategori Adı")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sıra numarası zorunludur.")]
    [Range(0, 9999, ErrorMessage = "Sıra numarası 0 ile 9999 arasında olmalıdır.")]
    [Display(Name = "Sıra")]
    public int SortOrder { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
