using System.ComponentModel.DataAnnotations;

namespace DukkanPilot.Web.Areas.Business.Models;

public class BusinessTablesIndexViewModel
{
    public string BusinessSlug { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
    public bool IsBusinessOwner { get; set; }
    public List<BusinessTableRowViewModel> Tables { get; set; } = [];
}

public class BusinessTableRowViewModel
{
    public int Id { get; set; }
    public string TableLabel { get; set; } = string.Empty;
    public string PublicCode { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public string PublicMenuUrl { get; set; } = string.Empty;
}

public class BusinessTableFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Masa adı zorunludur.")]
    [StringLength(80, ErrorMessage = "Masa adı en fazla 80 karakter olabilir.")]
    [Display(Name = "Masa adı")]
    public string TableLabel { get; set; } = string.Empty;

    [Display(Name = "Sıra")]
    [Range(0, 9999, ErrorMessage = "Sıra 0 ile 9999 arasında olmalıdır.")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    public string? PublicCode { get; set; }
}

public class BusinessTableQrViewModel
{
    public int Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string TableLabel { get; set; } = string.Empty;
    public string PublicCode { get; set; } = string.Empty;
    public string PublicMenuUrl { get; set; } = string.Empty;
    public string QrPayload { get; set; } = string.Empty;
    public string ThemeColor { get; set; } = "#2563eb";
    public string? LogoUrl { get; set; }
}
