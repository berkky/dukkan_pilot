using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DukkanPilot.Web.Areas.Business.Models;

public class RewardRedeemViewModel
{
    public int RewardId { get; set; }

    public string RewardName { get; set; } = string.Empty;

    public int RequiredPoints { get; set; }

    [Required(ErrorMessage = "Müşteri seçimi zorunludur.")]
    [Display(Name = "Müşteri")]
    public int CustomerId { get; set; }

    [MaxLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    public List<SelectListItem> AvailableCustomers { get; set; } = new();
}
