using System.ComponentModel.DataAnnotations;

namespace DukkanPilot.Web.Areas.Business.Models;

public class LoyaltyRuleFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Puan oranı zorunludur.")]
    [Range(0.01, 1_000_000, ErrorMessage = "Puan oranı 0'dan büyük olmalıdır.")]
    [Display(Name = "Puan Oranı (her minimum tutar başına)")]
    public decimal PointsPerAmount { get; set; } = 1m;

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
