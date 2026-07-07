using System.ComponentModel.DataAnnotations;

namespace DukkanPilot.Web.Areas.Business.Models;

public class RewardFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Ödül adı zorunludur.")]
    [MaxLength(150, ErrorMessage = "Ödül adı en fazla 150 karakter olabilir.")]
    [Display(Name = "Ödül Adı")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Gerekli puan zorunludur.")]
    [Range(1, 1_000_000, ErrorMessage = "Gerekli puan 1 ile 1000000 arasında olmalıdır.")]
    [Display(Name = "Gerekli Puan")]
    public int RequiredPoints { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
