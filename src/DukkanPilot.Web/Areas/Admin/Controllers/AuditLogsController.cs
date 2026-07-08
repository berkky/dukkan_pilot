using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessAuditLogsController = DukkanPilot.Web.Areas.Business.Controllers.AuditLogsController;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Route("Admin/AuditLogs")]
public class AuditLogsController : AdminBaseController
{
    private readonly AppDbContext _context;

    public AuditLogsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        int? businessId,
        string? search,
        string? area,
        string? action,
        string? severity,
        string? userEmail,
        DateTime? startDate,
        DateTime? endDate,
        int page = 1)
    {
        ViewData["ActiveMenu"] = "audit-logs";

        page = page < 1 ? 1 : page;
        const int pageSize = 100;

        var nowUtc = DateTime.UtcNow;
        var todayStart = DateTime.SpecifyKind(nowUtc.Date, DateTimeKind.Utc);
        var last7Start = todayStart.AddDays(-6);

        var baseQuery = _context.AuditLogs.AsNoTracking();

        var todayCount = await baseQuery.CountAsync(a => a.CreatedAtUtc >= todayStart);
        var last7Count = await baseQuery.CountAsync(a => a.CreatedAtUtc >= last7Start);
        var adminCount = await baseQuery.CountAsync(a => a.CreatedAtUtc >= last7Start && a.Area == "Admin");
        var criticalCount = await baseQuery.CountAsync(a =>
            a.CreatedAtUtc >= last7Start &&
            (a.Severity == "Critical" || a.Severity == "Warning"));
        var latest = await baseQuery.MaxAsync(a => (DateTime?)a.CreatedAtUtc);

        var filtered = BusinessAuditLogsController.ApplyFilters(
            baseQuery, search, action, severity, userEmail, startDate, endDate, area);

        if (businessId.HasValue && businessId.Value > 0)
        {
            filtered = filtered.Where(a => a.BusinessId == businessId.Value);
        }

        var totalCount = await filtered.CountAsync();

        var businessNames = await _context.Businesses.AsNoTracking()
            .Select(b => new { b.Id, b.Name })
            .ToDictionaryAsync(b => b.Id, b => b.Name);

        var rows = await filtered
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = rows.Select(a => new AuditLogRowViewModel
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
            MetadataJson = a.MetadataJson is { Length: > 300 }
                ? a.MetadataJson[..300] + "…"
                : a.MetadataJson,
            BusinessId = a.BusinessId,
            BusinessName = a.BusinessId.HasValue && businessNames.TryGetValue(a.BusinessId.Value, out var name)
                ? name
                : null
        }).ToList();

        var model = new AdminAuditLogIndexViewModel
        {
            Items = items,
            BusinessId = businessId,
            Search = search,
            Area = area,
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
            LatestActivityUtc = latest,
            AdminActionCount = adminCount
        };

        return View(model);
    }
}
