using System.ComponentModel.DataAnnotations;
using DukkanPilot.Core.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DukkanPilot.Web.Areas.Admin.Models;

public class BusinessSubscriptionEditViewModel : IValidatableObject
{
    public int? SubscriptionId { get; set; }

    public int BusinessId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string BusinessSlug { get; set; } = string.Empty;

    public string CurrentPlanName { get; set; } = "-";

    public string CurrentStatusText { get; set; } = "-";

    [Required(ErrorMessage = "Abonelik planı seçiniz.")]
    [Display(Name = "Abonelik Planı")]
    public int SubscriptionPlanId { get; set; }

    [Required(ErrorMessage = "Abonelik durumu seçiniz.")]
    [Display(Name = "Durum")]
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    [Display(Name = "Başlangıç Tarihi")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

    [Display(Name = "Bitiş Tarihi")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Aktif Kayıt")]
    public bool IsActive { get; set; } = true;

    public List<SelectListItem> AvailablePlans { get; set; } = new();

    public List<SelectListItem> AvailableStatuses { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate.HasValue && EndDate.Value.Date < StartDate.Date)
        {
            yield return new ValidationResult(
                "Bitiş tarihi başlangıç tarihinden önce olamaz.",
                [nameof(EndDate)]);
        }
    }
}
