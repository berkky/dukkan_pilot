namespace DukkanPilot.Web.Areas.Business.Models;

public class ProductImportCsvPageViewModel
{
    public string Mode { get; set; } = "preview";

    public ProductCsvImportResultViewModel? Result { get; set; }
}

public class ProductCsvImportResultViewModel
{
    public bool IsDryRun { get; set; }

    public bool HasFileError { get; set; }

    public string? FileErrorMessage { get; set; }

    public int TotalRows { get; set; }

    public int ValidRows { get; set; }

    public int ErrorRows { get; set; }

    public int ImportedRows { get; set; }

    public int SkippedRows { get; set; }

    public int SkippedByPlanLimitRows { get; set; }

    public int RemainingImportSlots { get; set; }

    public List<ProductCsvImportRowResultViewModel> PreviewRows { get; set; } = [];
}

public class ProductCsvImportRowResultViewModel
{
    public int RowNumber { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}
