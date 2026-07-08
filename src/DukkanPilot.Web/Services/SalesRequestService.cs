using System.Text.Json;
using DukkanPilot.Core.Entities;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Helpers;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Services;

public sealed class PublicSalesRequestCreateInput
{
    public string Source { get; set; } = "PublicDemo";
    public string RequestType { get; set; } = "DemoRequest";
    public string ContactName { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Message { get; set; }
    public int? RequestedPlanId { get; set; }
    public string? RequestedPlanName { get; set; }
    public bool PrivacyNoticeAcknowledged { get; set; }
    public bool KvkkNoticeAcknowledged { get; set; }
}

public sealed class BusinessSalesRequestCreateInput
{
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int? CurrentPlanId { get; set; }
    public string? CurrentPlanName { get; set; }
    public int RequestedPlanId { get; set; }
    public string RequestedPlanName { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public sealed class SalesRequestCreateResult
{
    public SalesRequest Request { get; set; } = null!;
    public bool WasDuplicate { get; set; }
}

public sealed class SalesRequestStatusUpdateInput
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public string? AdminNotes { get; set; }
    public string? ClosedReason { get; set; }
    public bool MarkContactedNow { get; set; }
}

public sealed class AdminSalesRequestSummary
{
    public int NewCount { get; set; }
    public int ContactedCount { get; set; }
    public int QualifiedCount { get; set; }
    public int WonCount { get; set; }
    public int LostCount { get; set; }
    public int Last7DaysCount { get; set; }
    public int OpenCount { get; set; }
}

public interface ISalesRequestService
{
    Task<SalesRequestCreateResult> CreatePublicRequestAsync(PublicSalesRequestCreateInput input, CancellationToken cancellationToken = default);
    Task<SalesRequestCreateResult> CreateBusinessPlanRequestAsync(BusinessSalesRequestCreateInput input, CancellationToken cancellationToken = default);
    Task<SalesRequest?> UpdateStatusAsync(SalesRequestStatusUpdateInput input, CancellationToken cancellationToken = default);
    Task<AdminSalesRequestSummary> GetAdminSummaryAsync(CancellationToken cancellationToken = default);
    Task<List<SalesRequest>> GetBusinessRequestsAsync(int businessId, CancellationToken cancellationToken = default);
    Task<SalesRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}

public class SalesRequestService : ISalesRequestService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditLogService _auditLog;
    private readonly INotificationService _notifications;

    public SalesRequestService(
        AppDbContext context,
        IHttpContextAccessor httpContextAccessor,
        IAuditLogService auditLog,
        INotificationService notifications)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _auditLog = auditLog;
        _notifications = notifications;
    }

    public async Task<SalesRequestCreateResult> CreatePublicRequestAsync(
        PublicSalesRequestCreateInput input,
        CancellationToken cancellationToken = default)
    {
        var email = Truncate(NormalizeEmail(input.Email), 200);
        var requestType = Truncate(input.RequestType, 40) ?? "DemoRequest";
        var source = Truncate(input.Source, 40) ?? "PublicDemo";

        var since = DateTime.UtcNow.AddHours(-24);
        var open = SalesRequestDisplayHelper.OpenStatuses;
        var existing = await _context.SalesRequests
            .Where(r => r.Email == email
                && r.RequestType == requestType
                && r.RequestedPlanId == input.RequestedPlanId
                && open.Contains(r.Status)
                && r.CreatedAtUtc >= since)
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return new SalesRequestCreateResult { Request = existing, WasDuplicate = true };
        }

        var entity = new SalesRequest
        {
            CreatedAtUtc = DateTime.UtcNow,
            Source = source,
            RequestType = requestType,
            Status = "New",
            Priority = "Normal",
            ContactName = Truncate(input.ContactName.Trim(), 120),
            BusinessName = Truncate(input.BusinessName.Trim(), 200),
            Email = email,
            Phone = Truncate(input.Phone?.Trim(), 40),
            Message = Truncate(input.Message?.Trim(), 2000),
            RequestedPlanId = input.RequestedPlanId,
            RequestedPlanName = Truncate(input.RequestedPlanName, 100),
            PrivacyNoticeAcknowledged = input.PrivacyNoticeAcknowledged,
            KvkkNoticeAcknowledged = input.KvkkNoticeAcknowledged,
            IpAddress = Truncate(GetIp(), 64),
            UserAgent = Truncate(GetUserAgent(), 400),
            MetadataJson = BuildMetadata(new { source, requestType, requestedPlanId = input.RequestedPlanId })
        };

        _context.SalesRequests.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _notifications.CreateAdminAsync(
                "SalesRequestCreated",
                "Yeni satış talebi",
                $"{entity.RequestType}: {entity.BusinessName} / {entity.ContactName}",
                $"/Admin/SalesRequests/Details/{entity.Id}",
                "Info",
                "SalesRequest",
                entity.Id,
                new { source = entity.Source, requestType = entity.RequestType, requestedPlanId = entity.RequestedPlanId },
                allowDuplicate: false,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // fail-safe
        }

        try
        {
            await _auditLog.LogAsync(new AuditLogEntry
            {
                BusinessId = null,
                Area = "Public",
                Action = "SalesRequest.Created",
                EntityName = "SalesRequest",
                EntityId = entity.Id,
                Summary = "Yeni public satış talebi oluşturuldu.",
                Metadata = new { source = entity.Source, requestType = entity.RequestType, requestedPlanId = entity.RequestedPlanId }
            }, cancellationToken);
        }
        catch
        {
            // fail-safe
        }

        return new SalesRequestCreateResult { Request = entity, WasDuplicate = false };
    }

    public async Task<SalesRequestCreateResult> CreateBusinessPlanRequestAsync(
        BusinessSalesRequestCreateInput input,
        CancellationToken cancellationToken = default)
    {
        var open = SalesRequestDisplayHelper.OpenStatuses;
        var existing = await _context.SalesRequests
            .Where(r => r.BusinessId == input.BusinessId
                && r.RequestedPlanId == input.RequestedPlanId
                && open.Contains(r.Status))
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return new SalesRequestCreateResult { Request = existing, WasDuplicate = true };
        }

        var isUpgrade = input.CurrentPlanId.HasValue && input.CurrentPlanId != input.RequestedPlanId;
        var entity = new SalesRequest
        {
            CreatedAtUtc = DateTime.UtcNow,
            BusinessId = input.BusinessId,
            Source = "BusinessBilling",
            RequestType = isUpgrade ? "UpgradeRequest" : "PlanRequest",
            Status = "New",
            Priority = "Normal",
            ContactName = Truncate(input.ContactName.Trim(), 120),
            BusinessName = Truncate(input.BusinessName.Trim(), 200),
            Email = Truncate(NormalizeEmail(input.Email), 200),
            Phone = Truncate(input.Phone?.Trim(), 40),
            Message = Truncate(input.Message?.Trim(), 2000),
            CurrentPlanId = input.CurrentPlanId,
            CurrentPlanName = Truncate(input.CurrentPlanName, 100),
            RequestedPlanId = input.RequestedPlanId,
            RequestedPlanName = Truncate(input.RequestedPlanName, 100),
            PrivacyNoticeAcknowledged = true,
            KvkkNoticeAcknowledged = true,
            IpAddress = Truncate(GetIp(), 64),
            UserAgent = Truncate(GetUserAgent(), 400),
            MetadataJson = BuildMetadata(new
            {
                source = "BusinessBilling",
                requestedPlanId = input.RequestedPlanId,
                currentPlanId = input.CurrentPlanId
            })
        };

        _context.SalesRequests.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _notifications.CreateAdminAsync(
                "SalesRequestCreated",
                "Yeni plan talebi",
                $"İşletme #{input.BusinessId}: {entity.CurrentPlanName} → {entity.RequestedPlanName}",
                $"/Admin/SalesRequests/Details/{entity.Id}",
                "Info",
                "SalesRequest",
                entity.Id,
                new { businessId = input.BusinessId, requestedPlanId = entity.RequestedPlanId },
                businessId: input.BusinessId,
                allowDuplicate: false,
                cancellationToken: cancellationToken);

            await _notifications.CreateBusinessAsync(
                input.BusinessId,
                "SalesRequestCreated",
                "Plan talebiniz alındı",
                $"{entity.RequestedPlanName} için talebiniz oluşturuldu. Durumu Satış Taleplerim ekranından izleyebilirsiniz.",
                "/Business/Billing/Requests",
                "Success",
                "SalesRequest",
                entity.Id,
                new { requestedPlanId = entity.RequestedPlanId },
                allowDuplicate: false,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // fail-safe
        }

        try
        {
            await _auditLog.LogBusinessAsync(
                input.BusinessId,
                "SalesRequest.Created",
                "SalesRequest",
                entity.Id,
                $"Plan talebi oluşturuldu: {entity.CurrentPlanName} → {entity.RequestedPlanName}",
                new { requestedPlanId = entity.RequestedPlanId, requestType = entity.RequestType });
        }
        catch
        {
            // fail-safe
        }

        return new SalesRequestCreateResult { Request = entity, WasDuplicate = false };
    }

    public async Task<SalesRequest?> UpdateStatusAsync(
        SalesRequestStatusUpdateInput input,
        CancellationToken cancellationToken = default)
    {
        if (!SalesRequestDisplayHelper.IsAllowedStatus(input.Status))
        {
            return null;
        }

        var priority = SalesRequestDisplayHelper.AllowedPriorities.Contains(input.Priority, StringComparer.Ordinal)
            ? input.Priority
            : "Normal";

        var entity = await _context.SalesRequests.FirstOrDefaultAsync(r => r.Id == input.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var oldStatus = entity.Status;
        entity.Status = input.Status;
        entity.Priority = priority;
        entity.AdminNotes = Truncate(input.AdminNotes?.Trim(), 2000);
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (input.MarkContactedNow
            || input.Status is "Contacted" or "Qualified" or "WaitingCustomer")
        {
            entity.LastContactedAtUtc ??= DateTime.UtcNow;
            if (input.MarkContactedNow)
            {
                entity.LastContactedAtUtc = DateTime.UtcNow;
            }
        }

        if (SalesRequestDisplayHelper.IsClosedStatus(input.Status))
        {
            entity.ClosedAtUtc ??= DateTime.UtcNow;
            entity.ClosedReason = Truncate(input.ClosedReason?.Trim(), 500);
        }
        else
        {
            entity.ClosedAtUtc = null;
            if (string.IsNullOrWhiteSpace(input.ClosedReason))
            {
                entity.ClosedReason = null;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLog.LogAdminAsync(
                "SalesRequest.StatusChanged",
                "SalesRequest",
                entity.Id,
                $"Satış talebi durumu güncellendi: {oldStatus} → {entity.Status}",
                new { oldStatus, newStatus = entity.Status, priority = entity.Priority },
                businessId: entity.BusinessId);
        }
        catch
        {
            // fail-safe
        }

        if (entity.BusinessId is int businessId)
        {
            try
            {
                await _notifications.CreateBusinessAsync(
                    businessId,
                    "SalesRequestUpdated",
                    "Plan talebiniz güncellendi",
                    $"Talebiniz {SalesRequestDisplayHelper.GetStatusLabel(entity.Status)} durumuna güncellendi.",
                    "/Business/Billing/Requests",
                    "Info",
                    "SalesRequest",
                    entity.Id,
                    new { status = entity.Status },
                    allowDuplicate: true,
                    cancellationToken: cancellationToken);
            }
            catch
            {
                // fail-safe
            }
        }

        return entity;
    }

    public async Task<AdminSalesRequestSummary> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-7);
        var rows = await _context.SalesRequests.AsNoTracking()
            .Select(r => new { r.Status, r.CreatedAtUtc })
            .ToListAsync(cancellationToken);

        return new AdminSalesRequestSummary
        {
            NewCount = rows.Count(r => r.Status == "New"),
            ContactedCount = rows.Count(r => r.Status == "Contacted"),
            QualifiedCount = rows.Count(r => r.Status == "Qualified"),
            WonCount = rows.Count(r => r.Status == "Won"),
            LostCount = rows.Count(r => r.Status == "Lost"),
            Last7DaysCount = rows.Count(r => r.CreatedAtUtc >= since),
            OpenCount = rows.Count(r => SalesRequestDisplayHelper.OpenStatuses.Contains(r.Status))
        };
    }

    public Task<List<SalesRequest>> GetBusinessRequestsAsync(int businessId, CancellationToken cancellationToken = default)
    {
        return _context.SalesRequests.AsNoTracking()
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<SalesRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.SalesRequests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    private string? GetIp()
    {
        var http = _httpContextAccessor.HttpContext;
        return http?.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        var http = _httpContextAccessor.HttpContext;
        return http?.Request.Headers.UserAgent.ToString();
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static string? BuildMetadata(object metadata)
    {
        try
        {
            var json = JsonSerializer.Serialize(metadata, JsonOptions);
            return json.Length <= 4000 ? json : json[..4000];
        }
        catch
        {
            return null;
        }
    }
}
