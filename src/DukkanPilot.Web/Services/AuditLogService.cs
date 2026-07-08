using System.Security.Claims;
using System.Text.Json;
using DukkanPilot.Core.Entities;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Constants;
using Microsoft.AspNetCore.Http;

namespace DukkanPilot.Web.Services;

public sealed class AuditLogEntry
{
    public int? BusinessId { get; set; }
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserRole { get; set; }
    public string Area { get; set; } = "Business";
    public string Action { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public int? EntityId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public object? Metadata { get; set; }
}

public interface IAuditLogService
{
    Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    Task LogBusinessAsync(
        int businessId,
        string action,
        string? entityName,
        int? entityId,
        string summary,
        object? metadata = null,
        string severity = "Info",
        CancellationToken cancellationToken = default);

    Task LogAdminAsync(
        string action,
        string? entityName,
        int? entityId,
        string summary,
        object? metadata = null,
        string severity = "Info",
        int? businessId = null,
        CancellationToken cancellationToken = default);

    Task LogAccountAsync(
        string action,
        string summary,
        object? metadata = null,
        string severity = "Info",
        int? businessId = null,
        string? userEmail = null,
        CancellationToken cancellationToken = default);

    Task LogPublicAsync(
        int businessId,
        string action,
        string? entityName,
        int? entityId,
        string summary,
        object? metadata = null,
        string severity = "Info",
        CancellationToken cancellationToken = default);
}

public class AuditLogService : IAuditLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "passwordhash", "token", "resettoken", "trackingtoken",
        "cookie", "authorization", "connectionstring", "secret", "apikey"
    };

    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task LogBusinessAsync(
        int businessId,
        string action,
        string? entityName,
        int? entityId,
        string summary,
        object? metadata = null,
        string severity = "Info",
        CancellationToken cancellationToken = default)
        => LogAsync(new AuditLogEntry
        {
            BusinessId = businessId,
            Area = "Business",
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Summary = summary,
            Metadata = metadata,
            Severity = severity
        }, cancellationToken);

    public Task LogAdminAsync(
        string action,
        string? entityName,
        int? entityId,
        string summary,
        object? metadata = null,
        string severity = "Info",
        int? businessId = null,
        CancellationToken cancellationToken = default)
        => LogAsync(new AuditLogEntry
        {
            BusinessId = businessId,
            Area = "Admin",
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Summary = summary,
            Metadata = metadata,
            Severity = severity
        }, cancellationToken);

    public Task LogAccountAsync(
        string action,
        string summary,
        object? metadata = null,
        string severity = "Info",
        int? businessId = null,
        string? userEmail = null,
        CancellationToken cancellationToken = default)
        => LogAsync(new AuditLogEntry
        {
            BusinessId = businessId,
            UserEmail = userEmail,
            Area = "Account",
            Action = action,
            Summary = summary,
            Metadata = metadata,
            Severity = severity
        }, cancellationToken);

    public Task LogPublicAsync(
        int businessId,
        string action,
        string? entityName,
        int? entityId,
        string summary,
        object? metadata = null,
        string severity = "Info",
        CancellationToken cancellationToken = default)
        => LogAsync(new AuditLogEntry
        {
            BusinessId = businessId,
            Area = "Public",
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Summary = summary,
            Metadata = metadata,
            Severity = severity
        }, cancellationToken);

    public async Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(entry.Action) || string.IsNullOrWhiteSpace(entry.Summary))
            {
                return;
            }

            var http = _httpContextAccessor.HttpContext;
            var user = http?.User;

            var userId = entry.UserId ?? TryParseInt(user?.FindFirstValue(ClaimTypes.NameIdentifier));
            var email = Truncate(entry.UserEmail ?? user?.FindFirstValue(ClaimTypes.Email) ?? user?.Identity?.Name, 200);
            var role = Truncate(entry.UserRole ?? user?.FindFirstValue(ClaimTypes.Role), 50);
            var businessId = entry.BusinessId
                ?? TryParseInt(user?.FindFirstValue(AuthClaimTypes.BusinessId));

            var log = new AuditLog
            {
                CreatedAtUtc = DateTime.UtcNow,
                BusinessId = businessId,
                UserId = userId,
                UserEmail = email,
                UserRole = role,
                Area = Truncate(entry.Area, 40) ?? "Business",
                Action = Truncate(entry.Action, 100)!,
                EntityName = Truncate(entry.EntityName, 80),
                EntityId = entry.EntityId,
                Summary = Truncate(entry.Summary, 500)!,
                IpAddress = Truncate(GetClientIp(http), 64),
                UserAgent = Truncate(http?.Request.Headers.UserAgent.ToString(), 400),
                MetadataJson = Truncate(SerializeMetadata(entry.Metadata), 4000),
                Severity = Truncate(string.IsNullOrWhiteSpace(entry.Severity) ? "Info" : entry.Severity, 20) ?? "Info"
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Audit failures must never break the primary business action.
        }
    }

    private static string? SerializeMetadata(object? metadata)
    {
        if (metadata is null)
        {
            return null;
        }

        try
        {
            if (metadata is IDictionary<string, object?> dict)
            {
                var sanitized = dict
                    .Where(kv => !SensitiveKeys.Contains(kv.Key))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
                return JsonSerializer.Serialize(sanitized, JsonOptions);
            }

            return JsonSerializer.Serialize(metadata, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetClientIp(HttpContext? http)
    {
        if (http is null)
        {
            return null;
        }

        var forwarded = http.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return http.Connection.RemoteIpAddress?.ToString();
    }

    private static int? TryParseInt(string? value)
        => int.TryParse(value, out var n) && n > 0 ? n : null;

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
