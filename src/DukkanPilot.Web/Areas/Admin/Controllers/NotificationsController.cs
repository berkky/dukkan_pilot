using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessNotificationsController = DukkanPilot.Web.Areas.Business.Controllers.NotificationsController;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Route("Admin/Notifications")]
public class NotificationsController : AdminBaseController
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
        int? businessId,
        string? search,
        string? type,
        string? severity,
        bool unreadOnly = false,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1)
    {
        ViewData["ActiveMenu"] = "notifications";

        await _notifications.GenerateSmartAdminAlertsAsync();

        page = page < 1 ? 1 : page;
        const int pageSize = 100;

        var nowUtc = DateTime.UtcNow;
        var todayStart = DateTime.SpecifyKind(nowUtc.Date, DateTimeKind.Utc);

        var baseQuery = _context.Notifications.AsNoTracking()
            .Where(n => n.Area == "Admin");

        var unreadCount = await baseQuery.CountAsync(n => !n.IsRead);
        var todayCount = await baseQuery.CountAsync(n => n.CreatedAtUtc >= todayStart);
        var criticalCount = await baseQuery.CountAsync(n => !n.IsRead && n.Severity == "Critical");
        var businessAlertCount = await baseQuery.CountAsync(n =>
            !n.IsRead && n.BusinessId != null);

        var filtered = BusinessNotificationsController.ApplyFilters(
            baseQuery, search, type, severity, unreadOnly, startDate, endDate, businessId);

        var totalCount = await filtered.CountAsync();

        var businessNames = await _context.Businesses.AsNoTracking()
            .Select(b => new { b.Id, b.Name })
            .ToDictionaryAsync(b => b.Id, b => b.Name);

        var rows = await filtered
            .OrderBy(n => n.IsRead)
            .ThenByDescending(n => n.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = rows.Select(n => new NotificationRowViewModel
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
            BusinessId = n.BusinessId,
            BusinessName = n.BusinessId.HasValue && businessNames.TryGetValue(n.BusinessId.Value, out var name)
                ? name
                : null
        }).ToList();

        var model = new AdminNotificationIndexViewModel
        {
            Items = items,
            BusinessId = businessId,
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
            WarningCount = await baseQuery.CountAsync(n => !n.IsRead && n.Severity == "Warning"),
            BusinessAlertCount = businessAlertCount
        };

        return View(model);
    }

    [HttpPost("MarkRead/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        await _notifications.MarkReadAsync(id, businessId: null, isAdmin: true);
        TempData["Success"] = "Bildirim okundu olarak işaretlendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("MarkAllRead")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notifications.MarkAllReadAsync(businessId: null, isAdmin: true);
        TempData["Success"] = "Tüm platform bildirimleri okundu olarak işaretlendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Summary")]
    [ResponseCache(NoStore = true, Duration = 0)]
    public async Task<IActionResult> Summary()
    {
        var query = _context.Notifications.AsNoTracking()
            .Where(n => n.Area == "Admin" && !n.IsRead);

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
}
