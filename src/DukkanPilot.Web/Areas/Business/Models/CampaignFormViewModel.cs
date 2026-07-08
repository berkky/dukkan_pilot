using System.ComponentModel.DataAnnotations;
using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Areas.Business.Models;

public class CampaignFormViewModel : IValidatableObject
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Kampanya başlığı zorunludur.")]
    [MaxLength(200, ErrorMessage = "Kampanya başlığı en fazla 200 karakter olabilir.")]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    [Display(Name = "Başlangıç Tarihi")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    [Display(Name = "Bitiş Tarihi")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.UtcNow.Date.AddMonths(1);

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "İndirim tipi zorunludur.")]
    [Display(Name = "İndirim Tipi")]
    public CampaignDiscountType DiscountType { get; set; } = CampaignDiscountType.Percentage;

    [Required(ErrorMessage = "İndirim değeri zorunludur.")]
    [Display(Name = "İndirim Değeri")]
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "İndirim değeri geçersiz.")]
    public decimal DiscountValue { get; set; }

    [Display(Name = "Minimum Sepet Tutarı")]
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Minimum sepet tutarı geçersiz.")]
    public decimal? MinimumOrderAmount { get; set; }

    [Display(Name = "Maksimum İndirim Tutarı")]
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Maksimum indirim tutarı geçersiz.")]
    public decimal? MaximumDiscountAmount { get; set; }

    [Display(Name = "Public menüde göster")]
    public bool IsPublicVisible { get; set; } = true;

    [Display(Name = "Sepette otomatik uygula")]
    public bool IsAutoApply { get; set; }

    [Display(Name = "Öncelik")]
    public int Priority { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate.Date < StartDate.Date)
        {
            yield return new ValidationResult(
                "Bitiş tarihi başlangıç tarihinden önce olamaz.",
                [nameof(EndDate)]);
        }

        if (DiscountType == CampaignDiscountType.None)
        {
            yield return new ValidationResult(
                "İndirim tipi seçilmelidir.",
                [nameof(DiscountType)]);
        }
        else if (DiscountType == CampaignDiscountType.Percentage)
        {
            if (DiscountValue < 0 || DiscountValue > 100)
            {
                yield return new ValidationResult(
                    "Yüzde indirimde değer 0 ile 100 arasında olmalıdır.",
                    [nameof(DiscountValue)]);
            }
        }
        else if (DiscountType == CampaignDiscountType.FixedAmount && DiscountValue < 0)
        {
            yield return new ValidationResult(
                "Sabit tutar indirimde değer 0 veya daha büyük olmalıdır.",
                [nameof(DiscountValue)]);
        }

        if (MinimumOrderAmount.HasValue && MinimumOrderAmount.Value < 0)
        {
            yield return new ValidationResult(
                "Minimum sepet tutarı 0 veya daha büyük olmalıdır.",
                [nameof(MinimumOrderAmount)]);
        }

        if (MaximumDiscountAmount.HasValue && MaximumDiscountAmount.Value < 0)
        {
            yield return new ValidationResult(
                "Maksimum indirim tutarı 0 veya daha büyük olmalıdır.",
                [nameof(MaximumDiscountAmount)]);
        }
    }
}
