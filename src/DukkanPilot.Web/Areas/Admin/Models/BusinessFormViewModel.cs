using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DukkanPilot.Web.Areas.Admin.Models;

public class BusinessFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "İşletme adı zorunludur.")]
    [MaxLength(200, ErrorMessage = "İşletme adı en fazla 200 karakter olabilir.")]
    [Display(Name = "İşletme Adı")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug zorunludur.")]
    [MaxLength(100, ErrorMessage = "Slug en fazla 100 karakter olabilir.")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug yalnızca küçük harf, rakam ve tire içerebilir.")]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir.")]
    [Display(Name = "Telefon")]
    public string? Phone { get; set; }

    [MaxLength(500, ErrorMessage = "Logo URL en fazla 500 karakter olabilir.")]
    [Display(Name = "Logo URL")]
    public string? LogoUrl { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [MaxLength(20, ErrorMessage = "WhatsApp numarası en fazla 20 karakter olabilir.")]
    [Display(Name = "WhatsApp Numarası")]
    public string? WhatsAppNumber { get; set; }

    [Required(ErrorMessage = "Tema rengi zorunludur.")]
    [MaxLength(20)]
    [Display(Name = "Tema Rengi")]
    public string ThemeColor { get; set; } = "#2563eb";

    [Required(ErrorMessage = "Para birimi zorunludur.")]
    [MaxLength(10)]
    [Display(Name = "Para Birimi")]
    public string Currency { get; set; } = "TRY";

    [Required(ErrorMessage = "Abonelik planı seçiniz.")]
    [Display(Name = "Abonelik Planı")]
    public int SubscriptionPlanId { get; set; }

    public List<SelectListItem> AvailablePlans { get; set; } = new();
}
