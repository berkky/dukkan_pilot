using System.Security.Cryptography;
using DukkanPilot.Core.Entities;
using DukkanPilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Helpers;

public static class BusinessTableCodeHelper
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static async Task<string> GenerateUniquePublicCodeAsync(AppDbContext context, int businessId)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var code = $"TBL-{GenerateSegment(4)}";
            var exists = await context.BusinessTables
                .AsNoTracking()
                .AnyAsync(t => t.BusinessId == businessId && t.PublicCode == code);

            if (!exists)
            {
                return code;
            }
        }

        return $"TBL-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
    }

    public static async Task<BusinessTable?> ResolveActiveTableAsync(
        AppDbContext context,
        int businessId,
        string? publicCode)
    {
        if (string.IsNullOrWhiteSpace(publicCode))
        {
            return null;
        }

        var normalized = publicCode.Trim();
        return await context.BusinessTables
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.BusinessId == businessId &&
                t.PublicCode == normalized &&
                t.IsActive);
    }

    public static string BuildTableMenuUrl(string baseUrl, string slug, string publicCode)
        => $"{baseUrl.TrimEnd('/')}/m/{slug}?table={Uri.EscapeDataString(publicCode)}";

    private static string GenerateSegment(int length)
    {
        Span<char> chars = stackalloc char[length];
        Span<byte> bytes = stackalloc byte[length];

        RandomNumberGenerator.Fill(bytes);
        for (var i = 0; i < length; i++)
        {
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return new string(chars);
    }
}
