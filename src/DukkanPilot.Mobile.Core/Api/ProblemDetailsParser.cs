using System.Net;
using System.Text.Json;
using DukkanPilot.Mobile.Core.Contracts;

namespace DukkanPilot.Mobile.Core.Api;

public static class ProblemDetailsParser
{
    public static async Task<MobileApiException> ParseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
            var root = document.RootElement;
            var code = GetString(root, "code") ?? DefaultCode(response.StatusCode);
            var traceId = GetString(root, "traceId");
            var errors = ReadErrors(root);
            var businesses = ReadBusinesses(root);
            return new MobileApiException(
                code,
                MobileErrorMessages.ForCode(code, response.StatusCode),
                response.StatusCode,
                traceId,
                errors,
                businesses);
        }
        catch (JsonException)
        {
            var code = DefaultCode(response.StatusCode);
            return new MobileApiException(
                code,
                MobileErrorMessages.ForCode(code, response.StatusCode),
                response.StatusCode);
        }
    }

    private static string DefaultCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => "unauthorized",
            HttpStatusCode.Forbidden => "forbidden",
            HttpStatusCode.NotFound => "resource_not_found",
            (HttpStatusCode)429 => "rate_limit_exceeded",
            _ => "api_error"
        };
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static IReadOnlyDictionary<string, string[]> ReadErrors(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out var errors) ||
            errors.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, string[]>();
        }

        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in errors.EnumerateObject())
        {
            result[property.Name] = property.Value.ValueKind == JsonValueKind.Array
                ? property.Value.EnumerateArray()
                    .Where(value => value.ValueKind == JsonValueKind.String)
                    .Select(value => value.GetString()!)
                    .ToArray()
                : [];
        }

        return result;
    }

    private static IReadOnlyList<MobileBusinessOption> ReadBusinesses(JsonElement root)
    {
        if (!root.TryGetProperty("businesses", out var businesses) ||
            businesses.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<MobileBusinessOption>();
        foreach (var business in businesses.EnumerateArray())
        {
            if (!business.TryGetProperty("id", out var id) ||
                !business.TryGetProperty("name", out var name) ||
                !business.TryGetProperty("role", out var role) ||
                id.ValueKind != JsonValueKind.Number ||
                name.ValueKind != JsonValueKind.String ||
                role.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            result.Add(new MobileBusinessOption(
                id.GetInt32(),
                name.GetString()!,
                role.GetString()!));
        }

        return result;
    }
}
