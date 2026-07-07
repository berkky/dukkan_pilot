using System.ComponentModel.DataAnnotations;
using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Areas.Business.Models;

public class StaffFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [MaxLength(200, ErrorMessage = "Ad soyad en fazla 200 karakter olabilir.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [MaxLength(256, ErrorMessage = "E-posta en fazla 256 karakter olabilir.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Şifre Tekrar")]
    public string? ConfirmPassword { get; set; }

    [Required(ErrorMessage = "İşletme rolü zorunludur.")]
    [Display(Name = "İşletme Rolü")]
    public BusinessRole BusinessRole { get; set; } = BusinessRole.Staff;

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    public bool IsEdit { get; set; }
}
