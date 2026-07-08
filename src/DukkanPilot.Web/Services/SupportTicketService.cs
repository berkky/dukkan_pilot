using System.Text.Json;
using DukkanPilot.Core.Entities;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Helpers;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Services;

public sealed class SupportTicketCreateInput
{
    public int BusinessId { get; set; }
    public int? CreatedByUserId { get; set; }
    public string? CreatedByUserEmail { get; set; }
    public string? CreatedByUserName { get; set; }
    public string Category { get; set; } = "Other";
    public string Priority { get; set; } = "Normal";
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = "BusinessPanel";
    public string? RelatedEntityName { get; set; }
    public int? RelatedEntityId { get; set; }
}

public sealed class SupportTicketMessageInput
{
    public int TicketId { get; set; }
    public int BusinessId { get; set; }
    public int? SenderUserId { get; set; }
    public string? SenderEmail { get; set; }
    public string? SenderName { get; set; }
    public string SenderRole { get; set; } = "Business";
    public string Message { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
}

public sealed class SupportTicketStatusUpdateInput
{
    public int TicketId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ResolutionSummary { get; set; }
}

public sealed class SupportTicketAssignInput
{
    public int TicketId { get; set; }
    public int? AssignedAdminUserId { get; set; }
    public string? AssignedAdminEmail { get; set; }
}

public sealed class BusinessSupportTicketSummary
{
    public int OpenCount { get; set; }
    public int WaitingCustomerCount { get; set; }
    public int ResolvedOrClosedCount { get; set; }
    public int UrgentOrHighCount { get; set; }
    public SupportTicket? LatestTicket { get; set; }
}

public sealed class AdminSupportTicketSummary
{
    public int NewCount { get; set; }
    public int OpenOrInProgressCount { get; set; }
    public int WaitingCustomerCount { get; set; }
    public int UrgentOrHighCount { get; set; }
    public int ResolvedThisMonthCount { get; set; }
    public int WaitingAdminCount { get; set; }
}

public interface ISupportTicketService
{
    Task<SupportTicket?> CreateTicketAsync(SupportTicketCreateInput input, CancellationToken cancellationToken = default);
    Task<SupportTicketMessage?> AddBusinessMessageAsync(SupportTicketMessageInput input, CancellationToken cancellationToken = default);
    Task<SupportTicketMessage?> AddAdminReplyAsync(SupportTicketMessageInput input, CancellationToken cancellationToken = default);
    Task<SupportTicketMessage?> AddAdminInternalNoteAsync(SupportTicketMessageInput input, CancellationToken cancellationToken = default);
    Task<SupportTicket?> UpdateTicketStatusAsync(SupportTicketStatusUpdateInput input, CancellationToken cancellationToken = default);
    Task<SupportTicket?> UpdateTicketPriorityAsync(int ticketId, string priority, CancellationToken cancellationToken = default);
    Task<SupportTicket?> AssignTicketAsync(SupportTicketAssignInput input, CancellationToken cancellationToken = default);
    Task<SupportTicket?> CloseTicketAsync(int ticketId, string? resolutionSummary = null, CancellationToken cancellationToken = default);
    Task<SupportTicket?> CloseByBusinessAsync(int ticketId, int businessId, string? resolutionSummary = null, CancellationToken cancellationToken = default);
    Task<BusinessSupportTicketSummary> GetBusinessTicketSummaryAsync(int businessId, CancellationToken cancellationToken = default);
    Task<AdminSupportTicketSummary> GetAdminTicketSummaryAsync(CancellationToken cancellationToken = default);
    Task<string> GenerateTicketNumberAsync(CancellationToken cancellationToken = default);
    Task<SupportTicket?> GetTicketForBusinessAsync(int ticketId, int businessId, CancellationToken cancellationToken = default);
    Task<SupportTicket?> GetTicketForAdminAsync(int ticketId, CancellationToken cancellationToken = default);
}

public class SupportTicketService : ISupportTicketService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly INotificationService _notifications;

    public SupportTicketService(
        AppDbContext context,
        IAuditLogService auditLog,
        INotificationService notifications)
    {
        _context = context;
        _auditLog = auditLog;
        _notifications = notifications;
    }

    public async Task<SupportTicket?> CreateTicketAsync(
        SupportTicketCreateInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.BusinessId <= 0
            || string.IsNullOrWhiteSpace(input.Subject)
            || string.IsNullOrWhiteSpace(input.Message))
        {
            return null;
        }

        var category = NormalizeCategory(input.Category);
        var priority = NormalizePriority(input.Priority);
        var source = NormalizeSource(input.Source);
        var now = DateTime.UtcNow;

        var ticket = new SupportTicket
        {
            CreatedAtUtc = now,
            BusinessId = input.BusinessId,
            CreatedByUserId = input.CreatedByUserId,
            CreatedByUserEmail = Truncate(input.CreatedByUserEmail, 256),
            CreatedByUserName = Truncate(input.CreatedByUserName, 200),
            TicketNumber = await GenerateTicketNumberAsync(cancellationToken),
            Subject = Truncate(input.Subject.Trim(), 300)!,
            Category = category,
            Priority = priority,
            Status = "New",
            Source = source,
            RelatedEntityName = Truncate(input.RelatedEntityName, 80),
            RelatedEntityId = input.RelatedEntityId,
            LastMessageAtUtc = now,
            LastMessageByRole = "Business"
        };

        var message = new SupportTicketMessage
        {
            CreatedAtUtc = now,
            SupportTicket = ticket,
            BusinessId = input.BusinessId,
            SenderUserId = input.CreatedByUserId,
            SenderEmail = Truncate(input.CreatedByUserEmail, 256),
            SenderName = Truncate(input.CreatedByUserName, 200),
            SenderRole = "Business",
            Message = Truncate(input.Message.Trim(), 4000)!,
            IsInternal = false,
            IsSystemMessage = false
        };

        _context.SupportTickets.Add(ticket);
        _context.SupportTicketMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        await TryAuditBusinessAsync(
            input.BusinessId,
            "Support.TicketCreated",
            ticket.Id,
            $"Destek talebi oluşturuldu: {ticket.TicketNumber}",
            new
            {
                ticketNumber = ticket.TicketNumber,
                category = ticket.Category,
                priority = ticket.Priority,
                status = ticket.Status,
                businessId = ticket.BusinessId,
                messageLength = message.Message.Length
            });

        await TryNotifyAdminAsync(
            "NewSupportTicket",
            "Yeni destek talebi",
            $"{ticket.TicketNumber}: {ticket.Subject}",
            $"/Admin/Support/Details/{ticket.Id}",
            "Info",
            ticket.Id,
            ticket.BusinessId,
            new { ticketNumber = ticket.TicketNumber, category = ticket.Category, priority = ticket.Priority });

        return ticket;
    }

    public async Task<SupportTicketMessage?> AddBusinessMessageAsync(
        SupportTicketMessageInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Message))
        {
            return null;
        }

        var ticket = await _context.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == input.TicketId && t.BusinessId == input.BusinessId, cancellationToken);
        if (ticket is null || SupportTicketDisplayHelper.IsClosedStatus(ticket.Status))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var message = new SupportTicketMessage
        {
            CreatedAtUtc = now,
            SupportTicketId = ticket.Id,
            BusinessId = input.BusinessId,
            SenderUserId = input.SenderUserId,
            SenderEmail = Truncate(input.SenderEmail, 256),
            SenderName = Truncate(input.SenderName, 200),
            SenderRole = "Business",
            Message = Truncate(input.Message.Trim(), 4000)!,
            IsInternal = false,
            IsSystemMessage = false
        };

        ticket.UpdatedAtUtc = now;
        ticket.LastMessageAtUtc = now;
        ticket.LastMessageByRole = "Business";
        if (ticket.Status is "WaitingCustomer" or "Resolved")
        {
            ticket.Status = "Open";
        }

        _context.SupportTicketMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        await TryAuditBusinessAsync(
            input.BusinessId,
            "Support.BusinessMessageAdded",
            ticket.Id,
            $"Destek talebine işletme mesajı eklendi: {ticket.TicketNumber}",
            new
            {
                ticketNumber = ticket.TicketNumber,
                category = ticket.Category,
                status = ticket.Status,
                businessId = ticket.BusinessId,
                messageLength = message.Message.Length
            });

        await TryNotifyAdminAsync(
            "SupportTicketUpdated",
            "Destek talebi güncellendi",
            $"{ticket.TicketNumber}: işletmeden yeni mesaj",
            $"/Admin/Support/Details/{ticket.Id}",
            "Info",
            ticket.Id,
            ticket.BusinessId,
            new { ticketNumber = ticket.TicketNumber, status = ticket.Status },
            allowDuplicate: true);

        return message;
    }

    public async Task<SupportTicketMessage?> AddAdminReplyAsync(
        SupportTicketMessageInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Message) || input.IsInternal)
        {
            return null;
        }

        var ticket = await _context.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == input.TicketId, cancellationToken);
        if (ticket is null || SupportTicketDisplayHelper.IsClosedStatus(ticket.Status))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var message = new SupportTicketMessage
        {
            CreatedAtUtc = now,
            SupportTicketId = ticket.Id,
            BusinessId = ticket.BusinessId,
            SenderUserId = input.SenderUserId,
            SenderEmail = Truncate(input.SenderEmail, 256),
            SenderName = Truncate(input.SenderName, 200),
            SenderRole = "Admin",
            Message = Truncate(input.Message.Trim(), 4000)!,
            IsInternal = false,
            IsSystemMessage = false
        };

        ticket.UpdatedAtUtc = now;
        ticket.LastMessageAtUtc = now;
        ticket.LastMessageByRole = "Admin";
        if (ticket.Status is "New" or "WaitingAdmin")
        {
            ticket.Status = "InProgress";
        }

        _context.SupportTicketMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        await TryAuditAdminAsync(
            "Support.AdminReplyAdded",
            ticket.Id,
            $"Destek talebine admin yanıtı eklendi: {ticket.TicketNumber}",
            new
            {
                ticketNumber = ticket.TicketNumber,
                category = ticket.Category,
                status = ticket.Status,
                businessId = ticket.BusinessId,
                messageLength = message.Message.Length
            },
            ticket.BusinessId);

        await TryNotifyBusinessAsync(
            ticket.BusinessId,
            "SupportTicketReplied",
            "Destek talebinize yanıt verildi",
            $"{ticket.TicketNumber}: {ticket.Subject}",
            $"/Business/Support/Details/{ticket.Id}",
            ticket.Id,
            new { ticketNumber = ticket.TicketNumber, status = ticket.Status });

        return message;
    }

    public async Task<SupportTicketMessage?> AddAdminInternalNoteAsync(
        SupportTicketMessageInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Message))
        {
            return null;
        }

        var ticket = await _context.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == input.TicketId, cancellationToken);
        if (ticket is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var message = new SupportTicketMessage
        {
            CreatedAtUtc = now,
            SupportTicketId = ticket.Id,
            BusinessId = ticket.BusinessId,
            SenderUserId = input.SenderUserId,
            SenderEmail = Truncate(input.SenderEmail, 256),
            SenderName = Truncate(input.SenderName, 200),
            SenderRole = "Admin",
            Message = Truncate(input.Message.Trim(), 4000)!,
            IsInternal = true,
            IsSystemMessage = false
        };

        ticket.UpdatedAtUtc = now;
        ticket.AdminInternalNote = Truncate(input.Message.Trim(), 2000);

        _context.SupportTicketMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        await TryAuditAdminAsync(
            "Support.InternalNoteAdded",
            ticket.Id,
            $"Destek talebine iç not eklendi: {ticket.TicketNumber}",
            new
            {
                ticketNumber = ticket.TicketNumber,
                category = ticket.Category,
                status = ticket.Status,
                businessId = ticket.BusinessId,
                messageLength = message.Message.Length
            },
            ticket.BusinessId);

        return message;
    }

    public async Task<SupportTicket?> UpdateTicketStatusAsync(
        SupportTicketStatusUpdateInput input,
        CancellationToken cancellationToken = default)
    {
        if (!SupportTicketDisplayHelper.IsAllowedStatus(input.Status))
        {
            return null;
        }

        var ticket = await _context.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == input.TicketId, cancellationToken);
        if (ticket is null)
        {
            return null;
        }

        var oldStatus = ticket.Status;
        ticket.Status = input.Status;
        ticket.UpdatedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(input.ResolutionSummary))
        {
            ticket.ResolutionSummary = Truncate(input.ResolutionSummary.Trim(), 2000);
        }

        if (SupportTicketDisplayHelper.IsClosedStatus(input.Status))
        {
            ticket.ClosedAtUtc ??= DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await TryAuditAdminAsync(
            "Support.StatusChanged",
            ticket.Id,
            $"Destek talebi durumu güncellendi: {oldStatus} → {ticket.Status}",
            new
            {
                ticketNumber = ticket.TicketNumber,
                category = ticket.Category,
                priority = ticket.Priority,
                oldStatus,
                newStatus = ticket.Status,
                businessId = ticket.BusinessId
            },
            ticket.BusinessId);

        if (SupportTicketDisplayHelper.ShouldNotifyBusinessOnStatusChange(ticket.Status))
        {
            var type = ticket.Status is "Resolved" or "Closed"
                ? "SupportTicketResolved"
                : "SupportTicketStatusChanged";

            await TryNotifyBusinessAsync(
                ticket.BusinessId,
                type,
                "Destek talebi durumu güncellendi",
                $"{ticket.TicketNumber}: {SupportTicketDisplayHelper.GetStatusLabel(ticket.Status)}",
                $"/Business/Support/Details/{ticket.Id}",
                ticket.Id,
                new { ticketNumber = ticket.TicketNumber, status = ticket.Status },
                allowDuplicate: true);
        }

        return ticket;
    }

    public async Task<SupportTicket?> UpdateTicketPriorityAsync(
        int ticketId,
        string priority,
        CancellationToken cancellationToken = default)
    {
        if (!SupportTicketDisplayHelper.IsAllowedPriority(priority))
        {
            return null;
        }

        var ticket = await _context.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);
        if (ticket is null)
        {
            return null;
        }

        var oldPriority = ticket.Priority;
        ticket.Priority = priority;
        ticket.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await TryAuditAdminAsync(
            "Support.PriorityChanged",
            ticket.Id,
            $"Destek talebi önceliği güncellendi: {oldPriority} → {ticket.Priority}",
            new
            {
                ticketNumber = ticket.TicketNumber,
                category = ticket.Category,
                oldPriority,
                newPriority = ticket.Priority,
                businessId = ticket.BusinessId
            },
            ticket.BusinessId);

        return ticket;
    }

    public async Task<SupportTicket?> AssignTicketAsync(
        SupportTicketAssignInput input,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _context.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == input.TicketId, cancellationToken);
        if (ticket is null)
        {
            return null;
        }

        ticket.AssignedAdminUserId = input.AssignedAdminUserId;
        ticket.AssignedAdminEmail = Truncate(input.AssignedAdminEmail, 256);
        ticket.UpdatedAtUtc = DateTime.UtcNow;
        if (ticket.Status is "New")
        {
            ticket.Status = "Open";
        }

        await _context.SaveChangesAsync(cancellationToken);

        await TryAuditAdminAsync(
            "Support.Assigned",
            ticket.Id,
            $"Destek talebi atandı: {ticket.TicketNumber}",
            new
            {
                ticketNumber = ticket.TicketNumber,
                assignedAdminUserId = ticket.AssignedAdminUserId,
                businessId = ticket.BusinessId
            },
            ticket.BusinessId);

        return ticket;
    }

    public async Task<SupportTicket?> CloseTicketAsync(
        int ticketId,
        string? resolutionSummary = null,
        CancellationToken cancellationToken = default)
    {
        return await UpdateTicketStatusAsync(new SupportTicketStatusUpdateInput
        {
            TicketId = ticketId,
            Status = "Closed",
            ResolutionSummary = resolutionSummary
        }, cancellationToken);
    }

    public async Task<SupportTicket?> CloseByBusinessAsync(
        int ticketId,
        int businessId,
        string? resolutionSummary = null,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _context.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.BusinessId == businessId, cancellationToken);
        if (ticket is null)
        {
            return null;
        }

        ticket.Status = "Closed";
        ticket.ClosedAtUtc = DateTime.UtcNow;
        ticket.UpdatedAtUtc = DateTime.UtcNow;
        ticket.ResolutionSummary = Truncate(resolutionSummary ?? "İşletme tarafından kapatıldı.", 2000);
        await _context.SaveChangesAsync(cancellationToken);

        await TryAuditBusinessAsync(
            businessId,
            "Support.Closed",
            ticket.Id,
            $"Destek talebi işletme tarafından kapatıldı: {ticket.TicketNumber}",
            new { ticketNumber = ticket.TicketNumber, status = ticket.Status, businessId });

        return ticket;
    }

    public async Task<BusinessSupportTicketSummary> GetBusinessTicketSummaryAsync(
        int businessId,
        CancellationToken cancellationToken = default)
    {
        var tickets = await _context.SupportTickets.AsNoTracking()
            .Where(t => t.BusinessId == businessId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var open = SupportTicketDisplayHelper.OpenStatuses;
        var high = SupportTicketDisplayHelper.HighPriorityStatuses;
        var closed = SupportTicketDisplayHelper.ClosedStatuses;

        return new BusinessSupportTicketSummary
        {
            OpenCount = tickets.Count(t => open.Contains(t.Status)),
            WaitingCustomerCount = tickets.Count(t => t.Status == "WaitingCustomer"),
            ResolvedOrClosedCount = tickets.Count(t => closed.Contains(t.Status)),
            UrgentOrHighCount = tickets.Count(t => open.Contains(t.Status) && high.Contains(t.Priority)),
            LatestTicket = tickets.FirstOrDefault()
        };
    }

    public async Task<AdminSupportTicketSummary> GetAdminTicketSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var tickets = await _context.SupportTickets.AsNoTracking().ToListAsync(cancellationToken);
        var open = SupportTicketDisplayHelper.OpenStatuses;
        var high = SupportTicketDisplayHelper.HighPriorityStatuses;

        return new AdminSupportTicketSummary
        {
            NewCount = tickets.Count(t => t.Status == "New"),
            OpenOrInProgressCount = tickets.Count(t => t.Status is "Open" or "InProgress"),
            WaitingCustomerCount = tickets.Count(t => t.Status == "WaitingCustomer"),
            WaitingAdminCount = tickets.Count(t => t.Status == "WaitingAdmin"),
            UrgentOrHighCount = tickets.Count(t => open.Contains(t.Status) && high.Contains(t.Priority)),
            ResolvedThisMonthCount = tickets.Count(t =>
                (t.Status == "Resolved" || t.Status == "Closed")
                && t.ClosedAtUtc >= monthStart)
        };
    }

    public async Task<string> GenerateTicketNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var prefix = $"SUP-{today:yyyyMMdd}-";

        var count = await _context.SupportTickets
            .CountAsync(t => t.CreatedAtUtc >= today && t.CreatedAtUtc < tomorrow, cancellationToken);

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var number = $"{prefix}{(count + 1 + attempt):D4}";
            var exists = await _context.SupportTickets.AnyAsync(t => t.TicketNumber == number, cancellationToken);
            if (!exists)
            {
                return number;
            }
        }

        return $"{prefix}{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
    }

    public async Task<SupportTicket?> GetTicketForBusinessAsync(
        int ticketId,
        int businessId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _context.SupportTickets.AsNoTracking()
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.BusinessId == businessId, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        ticket.Messages = ticket.Messages
            .Where(m => !m.IsInternal)
            .OrderBy(m => m.CreatedAtUtc)
            .ToList();

        return ticket;
    }

    public async Task<SupportTicket?> GetTicketForAdminAsync(int ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _context.SupportTickets.AsNoTracking()
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        ticket.Messages = ticket.Messages.OrderBy(m => m.CreatedAtUtc).ToList();
        return ticket;
    }

    private static string NormalizeCategory(string? category) =>
        SupportTicketDisplayHelper.IsAllowedCategory(category) ? category! : "Other";

    private static string NormalizePriority(string? priority) =>
        SupportTicketDisplayHelper.IsAllowedPriority(priority) ? priority! : "Normal";

    private static string NormalizeSource(string? source) =>
        !string.IsNullOrWhiteSpace(source) && SupportTicketDisplayHelper.AllowedSources.Contains(source, StringComparer.Ordinal)
            ? source
            : "BusinessPanel";

    private async Task TryAuditBusinessAsync(
        int businessId,
        string action,
        int ticketId,
        string summary,
        object metadata)
    {
        try
        {
            await _auditLog.LogBusinessAsync(businessId, action, "SupportTicket", ticketId, summary, metadata);
        }
        catch
        {
            // fail-safe
        }
    }

    private async Task TryAuditAdminAsync(
        string action,
        int ticketId,
        string summary,
        object metadata,
        int businessId)
    {
        try
        {
            await _auditLog.LogAdminAsync(action, "SupportTicket", ticketId, summary, metadata, businessId: businessId);
        }
        catch
        {
            // fail-safe
        }
    }

    private async Task TryNotifyAdminAsync(
        string type,
        string title,
        string message,
        string actionUrl,
        string severity,
        int ticketId,
        int businessId,
        object metadata,
        bool allowDuplicate = false)
    {
        try
        {
            await _notifications.CreateAdminAsync(
                type,
                title,
                message,
                actionUrl,
                severity,
                "SupportTicket",
                ticketId,
                metadata,
                businessId: businessId,
                allowDuplicate: allowDuplicate);
        }
        catch
        {
            // fail-safe
        }
    }

    private async Task TryNotifyBusinessAsync(
        int businessId,
        string type,
        string title,
        string message,
        string actionUrl,
        int ticketId,
        object metadata,
        bool allowDuplicate = false)
    {
        try
        {
            await _notifications.CreateBusinessAsync(
                businessId,
                type,
                title,
                message,
                actionUrl,
                "Info",
                "SupportTicket",
                ticketId,
                metadata,
                allowDuplicate: allowDuplicate);
        }
        catch
        {
            // fail-safe
        }
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
