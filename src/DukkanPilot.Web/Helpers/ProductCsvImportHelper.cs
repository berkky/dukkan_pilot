using System.Globalization;
using System.Text;
using DukkanPilot.Core.Entities;

namespace DukkanPilot.Web.Helpers;

public static class ProductCsvImportHelper
{
    public const int MaxRowCount = 500;
    public const int MaxFileSizeBytes = 1024 * 1024;

    public static readonly string[] RequiredHeaders =
    [
        "CategoryName",
        "ProductName",
        "Description",
        "Price",
        "IsActive"
    ];

    public static byte[] BuildTemplateBytes()
    {
        var sb = new StringBuilder();
        sb.AppendLine("CategoryName,ProductName,Description,Price,IsActive");
        sb.AppendLine("Kahveler,Latte,Sütlü kahve,120,true");
        sb.AppendLine("Tatlılar,Cheesecake,Frambuazlı cheesecake,150,true");
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public static ProductCsvParseOutcome ParseFile(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var content = reader.ReadToEnd();

        if (string.IsNullOrWhiteSpace(content))
        {
            return ProductCsvParseOutcome.Fail("CSV dosyası boş.");
        }

        var lines = SplitLines(content);
        if (lines.Count == 0)
        {
            return ProductCsvParseOutcome.Fail("CSV dosyası boş.");
        }

        var headerFields = ParseCsvLine(lines[0]);
        if (!TryMapHeaders(headerFields, out var headerMap, out var headerError))
        {
            return ProductCsvParseOutcome.Fail(headerError);
        }

        var rows = new List<ProductCsvParsedRow>();
        for (var i = 1; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            rows.Add(new ProductCsvParsedRow
            {
                RowNumber = i + 1,
                Fields = ParseCsvLine(line),
                HeaderMap = headerMap
            });
        }

        if (rows.Count == 0)
        {
            return ProductCsvParseOutcome.Fail("CSV dosyasında veri satırı bulunamadı.");
        }

        if (rows.Count > MaxRowCount)
        {
            return ProductCsvParseOutcome.Fail($"CSV dosyası en fazla {MaxRowCount} veri satırı içerebilir.");
        }

        return ProductCsvParseOutcome.Success(rows);
    }

    public static ProductCsvImportResult ProcessRows(
        IReadOnlyList<ProductCsvParsedRow> parsedRows,
        IReadOnlyDictionary<string, int> categoriesByNormalizedName,
        IReadOnlySet<string> existingProductNames,
        int maxImportableCount,
        bool applyImport)
    {
        var result = new ProductCsvImportResult();
        var seenNamesInFile = new HashSet<string>(StringComparer.Ordinal);
        var importSlotsRemaining = maxImportableCount;

        foreach (var parsed in parsedRows)
        {
            var row = MapRow(parsed);
            result.TotalRows++;
            result.PreviewRows.Add(row);

            if (!string.IsNullOrWhiteSpace(row.ErrorMessage))
            {
                row.Status = "Hata";
                result.ErrorRows++;
                continue;
            }

            var normalizedCategory = NormalizeKey(row.CategoryName);
            if (!categoriesByNormalizedName.TryGetValue(normalizedCategory, out var categoryId))
            {
                row.Status = "Hata";
                row.ErrorMessage = "Kategori bulunamadı. Önce kategoriyi oluşturun.";
                result.ErrorRows++;
                continue;
            }

            row.CategoryId = categoryId;

            if (seenNamesInFile.Contains(row.ProductName))
            {
                row.Status = "Hata";
                row.ErrorMessage = "Bu dosyada aynı ürün adı tekrar ediyor.";
                result.ErrorRows++;
                continue;
            }

            if (existingProductNames.Contains(row.ProductName))
            {
                row.Status = "Hata";
                row.ErrorMessage = "Bu ürün adı zaten mevcut.";
                result.ErrorRows++;
                continue;
            }

            seenNamesInFile.Add(row.ProductName);
            result.ValidRows++;

            if (!applyImport)
            {
                if (importSlotsRemaining <= 0)
                {
                    row.Status = "Atlanacak";
                    row.ErrorMessage = "Plan ürün limiti nedeniyle içe aktarılamaz.";
                    result.SkippedByPlanLimitRows++;
                }
                else
                {
                    row.Status = "Geçerli";
                    importSlotsRemaining--;
                }

                continue;
            }

            if (importSlotsRemaining <= 0)
            {
                row.Status = "Atlandı";
                row.ErrorMessage = "Plan ürün limiti nedeniyle atlandı.";
                result.SkippedByPlanLimitRows++;
                continue;
            }

            row.Status = "İçe aktarıldı";
            result.ImportedRows++;
            result.ProductsToCreate.Add(new Product
            {
                BusinessId = 0,
                CategoryId = categoryId,
                Name = row.ProductName,
                Description = row.Description,
                Price = row.Price,
                IsActive = row.IsActive,
                SortOrder = 0
            });

            importSlotsRemaining--;
        }

        return result;
    }

    private static ProductCsvImportRowResult MapRow(ProductCsvParsedRow parsed)
    {
        var row = new ProductCsvImportRowResult
        {
            RowNumber = parsed.RowNumber
        };

        string GetField(string name)
        {
            if (!parsed.HeaderMap.TryGetValue(name, out var index) || index >= parsed.Fields.Count)
            {
                return string.Empty;
            }

            return parsed.Fields[index].Trim();
        }

        row.CategoryName = GetField("categoryname");
        row.ProductName = GetField("productname");
        row.Description = string.IsNullOrWhiteSpace(GetField("description")) ? null : GetField("description");
        var priceText = GetField("price");
        var isActiveText = GetField("isactive");

        if (string.IsNullOrWhiteSpace(row.CategoryName))
        {
            row.ErrorMessage = "Kategori adı zorunludur.";
            return row;
        }

        if (string.IsNullOrWhiteSpace(row.ProductName))
        {
            row.ErrorMessage = "Ürün adı zorunludur.";
            return row;
        }

        row.ProductName = row.ProductName.Trim();

        if (!TryParsePrice(priceText, out var price, out var priceError))
        {
            row.ErrorMessage = priceError;
            return row;
        }

        row.Price = price;

        if (!TryParseIsActive(isActiveText, out var isActive, out var activeError))
        {
            row.ErrorMessage = activeError;
            return row;
        }

        row.IsActive = isActive;
        return row;
    }

    private static bool TryParsePrice(string text, out decimal price, out string error)
    {
        price = 0;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Fiyat zorunludur.";
            return false;
        }

        var normalized = text.Trim().Replace(" ", string.Empty);
        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out price) ||
            decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out price))
        {
            if (price < 0)
            {
                error = "Fiyat sıfırdan küçük olamaz.";
                return false;
            }

            return true;
        }

        error = "Fiyat geçersiz.";
        return false;
    }

    private static bool TryParseIsActive(string text, out bool isActive, out string error)
    {
        error = string.Empty;
        isActive = true;

        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var normalized = text.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "true":
            case "1":
            case "yes":
            case "evet":
            case "aktif":
                isActive = true;
                return true;
            case "false":
            case "0":
            case "no":
            case "hayir":
            case "hayır":
            case "pasif":
                isActive = false;
                return true;
            default:
                error = "IsActive değeri geçersiz. true/false kullanın.";
                return false;
        }
    }

    private static bool TryMapHeaders(
        IReadOnlyList<string> headerFields,
        out Dictionary<string, int> headerMap,
        out string error)
    {
        headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        error = string.Empty;

        if (headerFields.Count == 0)
        {
            error = "CSV başlık satırı bulunamadı.";
            return false;
        }

        for (var i = 0; i < headerFields.Count; i++)
        {
            var name = headerFields[i].Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            headerMap[name] = i;
        }

        foreach (var required in RequiredHeaders)
        {
            if (!headerMap.ContainsKey(required))
            {
                error = $"CSV başlık satırında '{required}' kolonu bulunamadı.";
                return false;
            }
        }

        return true;
    }

    private static List<string> SplitLines(string content)
    {
        return content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.None)
            .ToList();
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }

                continue;
            }

            if (c == '"')
            {
                inQuotes = true;
                continue;
            }

            if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        fields.Add(current.ToString());
        return fields;
    }

    private static string NormalizeKey(string value) => value.Trim().ToLowerInvariant();
}

public sealed class ProductCsvParsedRow
{
    public int RowNumber { get; init; }

    public List<string> Fields { get; init; } = [];

    public Dictionary<string, int> HeaderMap { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ProductCsvParseOutcome
{
    public bool IsSuccess { get; init; }

    public string? ErrorMessage { get; init; }

    public List<ProductCsvParsedRow> Rows { get; init; } = [];

    public static ProductCsvParseOutcome Success(List<ProductCsvParsedRow> rows) =>
        new() { IsSuccess = true, Rows = rows };

    public static ProductCsvParseOutcome Fail(string message) =>
        new() { IsSuccess = false, ErrorMessage = message };
}

public sealed class ProductCsvImportResult
{
    public int TotalRows { get; set; }

    public int ValidRows { get; set; }

    public int ErrorRows { get; set; }

    public int ImportedRows { get; set; }

    public int SkippedByPlanLimitRows { get; set; }

    public List<ProductCsvImportRowResult> PreviewRows { get; } = [];

    public List<Product> ProductsToCreate { get; } = [];
}

public sealed class ProductCsvImportRowResult
{
    public int RowNumber { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;

    public int? CategoryId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}
