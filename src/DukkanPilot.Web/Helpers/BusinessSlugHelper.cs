using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DukkanPilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Helpers;

public static partial class BusinessSlugHelper
{
    private static readonly Dictionary<char, char> TurkishCharMap = new()
    {
        ['ç'] = 'c', ['Ç'] = 'c',
        ['ğ'] = 'g', ['Ğ'] = 'g',
        ['ı'] = 'i', ['I'] = 'i', ['İ'] = 'i',
        ['ö'] = 'o', ['Ö'] = 'o',
        ['ş'] = 's', ['Ş'] = 's',
        ['ü'] = 'u', ['Ü'] = 'u'
    };

    public static string GenerateSlug(string businessName)
    {
        if (string.IsNullOrWhiteSpace(businessName))
        {
            return "isletme";
        }

        var builder = new StringBuilder(businessName.Trim().Length);
        foreach (var character in businessName.Trim())
        {
            builder.Append(TurkishCharMap.TryGetValue(character, out var mapped)
                ? mapped
                : character);
        }

        var normalized = builder.ToString().Normalize(NormalizationForm.FormD);
        var ascii = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category is UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            ascii.Append(char.ToLowerInvariant(character));
        }

        var slug = NonAlphaNumericRegex().Replace(ascii.ToString(), "-");
        slug = MultiDashRegex().Replace(slug, "-").Trim('-');

        return string.IsNullOrWhiteSpace(slug) ? "isletme" : slug;
    }

    public static async Task<string> GenerateUniqueSlugAsync(
        AppDbContext context,
        string businessName,
        CancellationToken cancellationToken = default)
    {
        var baseSlug = GenerateSlug(businessName);
        var slug = baseSlug;
        var counter = 2;

        while (await context.Businesses.AnyAsync(b => b.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex NonAlphaNumericRegex();

    [GeneratedRegex(@"-+", RegexOptions.Compiled)]
    private static partial Regex MultiDashRegex();
}
