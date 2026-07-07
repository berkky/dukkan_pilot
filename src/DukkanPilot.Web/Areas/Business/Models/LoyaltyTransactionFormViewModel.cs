using System.ComponentModel.DataAnnotations;
using DukkanPilot.Core.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DukkanPilot.Web.Areas.Business.Models;

public class LoyaltyTransactionFormViewModel
{
    [Required(ErrorMessage = "Müşteri seçimi zorunludur.")]
    [Display(Name = "Müşteri")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Puan zorunludur.")]
    [Range(1, 1_000_000, ErrorMessage = "Puan 1 ile 1000000 arasında olmalıdır.")]
    [Display(Name = "Puan")]
    public int Points { get; set; }

    [Required(ErrorMessage = "İşlem tipi zorunludur.")]
    [Display(Name = "İşlem Tipi")]
    public LoyaltyTransactionType Type { get; set; } = LoyaltyTransactionType.Earn;

    [MaxLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    public List<SelectListItem> AvailableCustomers { get; set; } = new();
}
