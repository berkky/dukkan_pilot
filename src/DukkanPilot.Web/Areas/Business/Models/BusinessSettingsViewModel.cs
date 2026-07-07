using System.ComponentModel.DataAnnotations;

namespace DukkanPilot.Web.Areas.Business.Models;

public class BusinessSettingsViewModel
{
    public int BusinessId { get; set; }

    [Required(ErrorMessage = "İşletme adı zorunludur.")]
    [MaxLength(200, ErrorMessage = "İşletme adı en fazla 200 karakter olabilir.")]
    [Display(Name = "İşletme Adı")]
    public string BusinessName { get; set; } = string.Empty;

    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir.")]
    [Display(Name = "Telefon")]
    public string? Phone { get; set; }

    [MaxLength(500, ErrorMessage = "Logo URL en fazla 500 karakter olabilir.")]
    [Display(Name = "Logo URL")]
    public string? LogoUrl { get; set; }

    [MaxLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
    [Display(Name = "Adres")]
    public string? Address { get; set; }

    [MaxLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Durum")]
    public bool IsActive { get; set; }

    [MaxLength(20, ErrorMessage = "WhatsApp numarası en fazla 20 karakter olabilir.")]
    [Display(Name = "WhatsApp Numarası")]
    public string? WhatsAppNumber { get; set; }

    [Required(ErrorMessage = "Para birimi zorunludur.")]
    [MaxLength(10, ErrorMessage = "Para birimi en fazla 10 karakter olabilir.")]
    [Display(Name = "Para Birimi")]
    public string Currency { get; set; } = "TRY";

    [Required(ErrorMessage = "Tema rengi zorunludur.")]
    [MaxLength(20)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Tema rengi #RRGGBB formatında olmalıdır.")]
    [Display(Name = "Tema Rengi")]
    public string ThemeColor { get; set; } = "#2563eb";

    public string PublicMenuPath => $"/m/{Slug}";
}
