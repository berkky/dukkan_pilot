using System.ComponentModel.DataAnnotations;

namespace DukkanPilot.Web.Areas.Admin.Models;

public class SubscriptionPlanFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Plan adı zorunludur.")]
    [MaxLength(100, ErrorMessage = "Plan adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Plan Adı")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Fiyat zorunludur.")]
    [Range(0, 999999.99, ErrorMessage = "Fiyat 0 veya daha büyük olmalıdır.")]
    [Display(Name = "Fiyat (₺)")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Maksimum ürün sayısı zorunludur.")]
    [Range(1, 10000, ErrorMessage = "Maksimum ürün sayısı 1 ile 10000 arasında olmalıdır.")]
    [Display(Name = "Maks. Ürün")]
    public int MaxProducts { get; set; }

    [Required(ErrorMessage = "Maksimum kampanya sayısı zorunludur.")]
    [Range(0, 1000, ErrorMessage = "Maksimum kampanya sayısı 0 ile 1000 arasında olmalıdır.")]
    [Display(Name = "Maks. Kampanya")]
    public int MaxCampaigns { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
