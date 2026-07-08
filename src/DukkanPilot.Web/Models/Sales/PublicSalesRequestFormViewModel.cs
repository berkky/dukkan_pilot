using System.ComponentModel.DataAnnotations;

namespace DukkanPilot.Web.Models.Sales;

public class PublicSalesRequestFormViewModel
{
    public string FormMode { get; set; } = "Demo"; // Demo | Plan

    [Required(ErrorMessage = "Ad soyad gerekli.")]
    [StringLength(120)]
    [Display(Name = "Ad Soyad")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "İşletme adı gerekli.")]
    [StringLength(200)]
    [Display(Name = "İşletme adı")]
    public string BusinessName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta gerekli.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [StringLength(200)]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [StringLength(40)]
    [Display(Name = "Telefon")]
    public string? Phone { get; set; }

    [StringLength(2000)]
    [Display(Name = "Mesaj")]
    public string? Message { get; set; }

    public int? RequestedPlanId { get; set; }
    public string? RequestedPlanName { get; set; }
    public decimal? RequestedPlanPrice { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Gizlilik Politikası bağlantısını incelediğinizi onaylayın.")]
    [Display(Name = "Gizlilik politikası")]
    public bool PrivacyNoticeAcknowledged { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "KVKK Aydınlatma Metni bağlantısını incelediğinizi onaylayın.")]
    [Display(Name = "KVKK aydınlatma")]
    public bool KvkkNoticeAcknowledged { get; set; }
}
