using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Notifications")]
public class NotificationsController : BusinessBaseController
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notifications;

    public NotificationsController(AppDbContext context, INotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search,
        string? type,
        string? severity,
        bool unreadOnly = false,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1)
    {
        ViewData["ActiveMenu"] = "notifications";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        await _notifications.GenerateSmartBusinessAlertsAsync(businessId);

        page = page < 1 ? 1 : page;
        const int pageSize = 50;

        var nowUtc = DateTime.UtcNow;
        var todayStart = DateTime.SpecifyKind(nowUtc.Date, DateTimeKind.Utc);

        var baseQuery = _context.Notifications.AsNoTracking()
            .Where(n => n.BusinessId == businessId);

        var unreadCount = await baseQuery.CountAsync(n => !n.IsRead);
        var todayCount = await baseQuery.CountAsync(n => n.CreatedAtUtc >= todayStart);
        var criticalCount = await baseQuery.CountAsync(n => !n.IsRead && n.Severity == "Critical");
        var warningCount = await baseQuery.CountAsync(n => !n.IsRead && n.Severity == "Warning");

        var filtered = ApplyFilters(baseQuery, search, type, severity, unreadOnly, startDate, endDate, businessId: null);
        var totalCount = await filtered.CountAsync();

        var items = await filtered
            .OrderBy(n => n.IsRead)
            .ThenByDescending(n => n.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationRowViewModel
            {
                Id = n.Id,
                CreatedAtUtc = n.CreatedAtUtc,
                Area = n.Area,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                ActionUrl = n.ActionUrl,
                EntityName = n.EntityName,
                EntityId = n.EntityId,
                Severity = n.Severity,
                IsRead = n.IsRead,
                ReadAtUtc = n.ReadAtUtc,
                BusinessId = n.BusinessId
            })
            .ToListAsync();

        var model = new NotificationIndexViewModel
        {
            Items = items,
            Search = search,
            Type = type,
            Severity = severity,
            UnreadOnly = unreadOnly,
            StartDate = startDate,
            EndDate = endDate,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            UnreadCount = unreadCount,
            TodayCount = todayCount,
            CriticalCount = criticalCount,
            WarningCount = warningCount
        };

        return View(model);
    }

    [HttpPost("MarkRead/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        await _notifications.MarkReadAsync(id, businessId, isAdmin: false);
        TempData["Success"] = "Bildirim okundu olarak işaretlendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("MarkAllRead")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        await _notifications.MarkAllReadAsync(businessId, isAdmin: false);
        TempData["Success"] = "Tüm bildirimler okundu olarak işaretlendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Summary")]
    [ResponseCache(NoStore = true, Duration = 0)]
    public async Task<IActionResult> Summary()
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var query = _context.Notifications.AsNoTracking()
            .Where(n => n.BusinessId == businessId && !n.IsRead);

        var unreadCount = await query.CountAsync();
        var criticalCount = await query.CountAsync(n => n.Severity == "Critical");
        var latest = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => new { n.Title, n.ActionUrl })
            .FirstOrDefaultAsync();

        return Json(new NotificationSummaryViewModel
        {
            UnreadCount = unreadCount,
            CriticalCount = criticalCount,
            LatestTitle = latest?.Title,
            LatestUrl = latest?.ActionUrl
        });
    }

    internal static IQueryable<Core.Entities.Notification> ApplyFilters(
        IQueryable<Core.Entities.Notification> query,
        string? search,
        string? type,
        string? severity,
        bool unreadOnly,
        DateTime? startDate,
        DateTime? endDate,
        int? businessId)
    {
        if (businessId.HasValue && businessId.Value > 0)
        {
            query = query.Where(n => n.BusinessId == businessId.Value);
        }

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(n =>
                n.Title.Contains(term) ||
                n.Message.Contains(term) ||
                n.Type.Contains(term) ||
                (n.EntityName != null && n.EntityName.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var t = type.Trim();
            query = query.Where(n => n.Type == t);
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            var sev = severity.Trim();
            query = query.Where(n => n.Severity == sev);
        }

        if (startDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(n => n.CreatedAtUtc >= startUtc);
        }

        if (endDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(n => n.CreatedAtUtc < endUtc);
        }

        return query;
    }
}
