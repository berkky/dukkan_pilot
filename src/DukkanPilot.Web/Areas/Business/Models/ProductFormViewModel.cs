using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DukkanPilot.Web.Areas.Business.Models;

public class ProductFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Kategori seçiniz.")]
    [Display(Name = "Kategori")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Ürün adı zorunludur.")]
    [MaxLength(200, ErrorMessage = "Ürün adı en fazla 200 karakter olabilir.")]
    [Display(Name = "Ürün Adı")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Fiyat zorunludur.")]
    [Range(0.01, 999999.99, ErrorMessage = "Fiyat 0'dan büyük olmalıdır.")]
    [Display(Name = "Fiyat (₺)")]
    public decimal Price { get; set; }

    [MaxLength(500, ErrorMessage = "Görsel URL en fazla 500 karakter olabilir.")]
    [Display(Name = "Görsel URL")]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "Sıra numarası zorunludur.")]
    [Range(0, 9999, ErrorMessage = "Sıra numarası 0 ile 9999 arasında olmalıdır.")]
    [Display(Name = "Sıra")]
    public int SortOrder { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    public List<SelectListItem> AvailableCategories { get; set; } = new();
}
