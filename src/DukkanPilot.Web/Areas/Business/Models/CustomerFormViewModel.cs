using System.ComponentModel.DataAnnotations;

namespace DukkanPilot.Web.Areas.Business.Models;

public class CustomerFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Müşteri adı zorunludur.")]
    [MaxLength(200, ErrorMessage = "Müşteri adı en fazla 200 karakter olabilir.")]
    [Display(Name = "Müşteri Adı")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon zorunludur.")]
    [MaxLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir.")]
    [Display(Name = "Telefon")]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Notlar en fazla 1000 karakter olabilir.")]
    [Display(Name = "Notlar")]
    public string? Notes { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "Toplam puan 0 ile 1000000 arasında olmalıdır.")]
    [Display(Name = "Toplam Puan")]
    public int TotalPoints { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
