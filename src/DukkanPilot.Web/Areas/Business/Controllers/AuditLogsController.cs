using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/AuditLogs")]
[RequireActiveSubscription]
public class AuditLogsController : BusinessBaseController
{
    private readonly AppDbContext _context;

    public AuditLogsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? search,
        string? action,
        string? severity,
        string? userEmail,
        DateTime? startDate,
        DateTime? endDate,
        int page = 1)
    {
        ViewData["ActiveMenu"] = "audit-logs";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        page = page < 1 ? 1 : page;
        const int pageSize = 50;

        var nowUtc = DateTime.UtcNow;
        var todayStart = DateTime.SpecifyKind(nowUtc.Date, DateTimeKind.Utc);
        var last7Start = todayStart.AddDays(-6);

        var baseQuery = _context.AuditLogs.AsNoTracking()
            .Where(a => a.BusinessId == businessId);

        var todayCount = await baseQuery.CountAsync(a => a.CreatedAtUtc >= todayStart);
        var last7Count = await baseQuery.CountAsync(a => a.CreatedAtUtc >= last7Start);
        var criticalCount = await baseQuery.CountAsync(a =>
            a.CreatedAtUtc >= last7Start &&
            (a.Severity == "Critical" || a.Severity == "Warning"));
        var latest = await baseQuery.MaxAsync(a => (DateTime?)a.CreatedAtUtc);

        var filtered = ApplyFilters(baseQuery, search, action, severity, userEmail, startDate, endDate, area: null);
        var totalCount = await filtered.CountAsync();

        var items = await filtered
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogRowViewModel
            {
                Id = a.Id,
                CreatedAtUtc = a.CreatedAtUtc,
                UserEmail = a.UserEmail,
                UserRole = a.UserRole,
                Area = a.Area,
                Action = a.Action,
                Summary = a.Summary,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Severity = a.Severity,
                MetadataJson = a.MetadataJson,
                BusinessId = a.BusinessId
            })
            .ToListAsync();

        var model = new AuditLogIndexViewModel
        {
            Items = items,
            Search = search,
            Action = action,
            Severity = severity,
            UserEmail = userEmail,
            StartDate = startDate,
            EndDate = endDate,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TodayCount = todayCount,
            Last7DaysCount = last7Count,
            CriticalCount = criticalCount,
            LatestActivityUtc = latest
        };

        return View(model);
    }

    internal static IQueryable<Core.Entities.AuditLog> ApplyFilters(
        IQueryable<Core.Entities.AuditLog> query,
        string? search,
        string? action,
        string? severity,
        string? userEmail,
        DateTime? startDate,
        DateTime? endDate,
        string? area)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a =>
                a.Summary.Contains(term) ||
                a.Action.Contains(term) ||
                (a.UserEmail != null && a.UserEmail.Contains(term)) ||
                (a.EntityName != null && a.EntityName.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var act = action.Trim();
            query = query.Where(a => a.Action.Contains(act));
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            var sev = severity.Trim();
            query = query.Where(a => a.Severity == sev);
        }

        if (!string.IsNullOrWhiteSpace(userEmail))
        {
            var email = userEmail.Trim();
            query = query.Where(a => a.UserEmail != null && a.UserEmail.Contains(email));
        }

        if (!string.IsNullOrWhiteSpace(area))
        {
            var ar = area.Trim();
            query = query.Where(a => a.Area == ar);
        }

        if (startDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(a => a.CreatedAtUtc >= startUtc);
        }

        if (endDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(a => a.CreatedAtUtc < endUtc);
        }

        return query;
    }
}
