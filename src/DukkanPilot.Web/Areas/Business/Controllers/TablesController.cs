using System.Text.RegularExpressions;
using DukkanPilot.Core.Entities;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Filters;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserRole = DukkanPilot.Core.Enums.UserRole;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Tables")]
[RequireActiveSubscription]
public class TablesController : BusinessBaseController
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;

    public TablesController(AppDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "tables";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business is null)
        {
            return NotFound();
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var isOwner = User.IsInRole(nameof(UserRole.BusinessOwner));

        var tables = await _context.BusinessTables
            .AsNoTracking()
            .Where(t => t.BusinessId == businessId)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.TableLabel)
            .Select(t => new BusinessTableRowViewModel
            {
                Id = t.Id,
                TableLabel = t.TableLabel,
                PublicCode = t.PublicCode,
                DisplayOrder = t.DisplayOrder,
                IsActive = t.IsActive
            })
            .ToListAsync();

        foreach (var table in tables)
        {
            table.PublicMenuUrl = BusinessTableCodeHelper.BuildTableMenuUrl(baseUrl, business.Slug, table.PublicCode);
        }

        var model = new BusinessTablesIndexViewModel
        {
            BusinessSlug = business.Slug,
            PublicBaseUrl = baseUrl,
            IsBusinessOwner = isOwner,
            Tables = tables
        };

        ViewData["IsBusinessOwner"] = isOwner;
        return View(model);
    }

    [HttpGet("Create")]
    [Authorize(Roles = nameof(UserRole.BusinessOwner))]
    public IActionResult Create()
    {
        ViewData["ActiveMenu"] = "tables-create";
        return View(new BusinessTableFormViewModel());
    }

    [HttpPost("Create")]
    [Authorize(Roles = nameof(UserRole.BusinessOwner))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BusinessTableFormViewModel model)
    {
        ViewData["ActiveMenu"] = "tables-create";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var publicCode = await BusinessTableCodeHelper.GenerateUniquePublicCodeAsync(_context, businessId);
        var table = new BusinessTable
        {
            BusinessId = businessId,
            TableLabel = model.TableLabel.Trim(),
            PublicCode = publicCode,
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive
        };

        _context.BusinessTables.Add(table);
        await _context.SaveChangesAsync();

        await _auditLog.LogBusinessAsync(
            businessId,
            "Table.Created",
            "BusinessTable",
            table.Id,
            $"Masa oluşturuldu: {table.TableLabel}",
            new { tableId = table.Id, tableLabel = table.TableLabel, publicCode = table.PublicCode });

        TempData["Success"] = $"\"{table.TableLabel}\" masası oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    [Authorize(Roles = nameof(UserRole.BusinessOwner))]
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["ActiveMenu"] = "tables";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var table = await _context.BusinessTables
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.BusinessId == businessId);

        if (table is null)
        {
            return NotFound();
        }

        var model = new BusinessTableFormViewModel
        {
            Id = table.Id,
            TableLabel = table.TableLabel,
            DisplayOrder = table.DisplayOrder,
            IsActive = table.IsActive,
            PublicCode = table.PublicCode
        };

        return View(model);
    }

    [HttpPost("Edit/{id:int}")]
    [Authorize(Roles = nameof(UserRole.BusinessOwner))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BusinessTableFormViewModel model)
    {
        ViewData["ActiveMenu"] = "tables";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (model.Id != id)
        {
            return BadRequest();
        }

        var table = await _context.BusinessTables
            .FirstOrDefaultAsync(t => t.Id == id && t.BusinessId == businessId);

        if (table is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.PublicCode = table.PublicCode;
            return View(model);
        }

        table.TableLabel = model.TableLabel.Trim();
        table.DisplayOrder = model.DisplayOrder;
        table.IsActive = model.IsActive;
        table.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLog.LogBusinessAsync(
            businessId,
            "Table.Updated",
            "BusinessTable",
            table.Id,
            $"Masa güncellendi: {table.TableLabel}",
            new { tableId = table.Id, tableLabel = table.TableLabel, isActive = table.IsActive });

        TempData["Success"] = $"\"{table.TableLabel}\" masası güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Toggle/{id:int}")]
    [Authorize(Roles = nameof(UserRole.BusinessOwner))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var table = await _context.BusinessTables
            .FirstOrDefaultAsync(t => t.Id == id && t.BusinessId == businessId);

        if (table is null)
        {
            return NotFound();
        }

        table.IsActive = !table.IsActive;
        table.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLog.LogBusinessAsync(
            businessId,
            "Table.Toggled",
            "BusinessTable",
            table.Id,
            $"Masa {(table.IsActive ? "aktif" : "pasif")}: {table.TableLabel}",
            new { tableId = table.Id, isActive = table.IsActive });

        TempData["Success"] = $"\"{table.TableLabel}\" {(table.IsActive ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Qr/{id:int}")]
    public async Task<IActionResult> Qr(int id)
    {
        ViewData["ActiveMenu"] = "tables";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var business = await _context.Businesses
            .AsNoTracking()
            .Include(b => b.Setting)
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business is null)
        {
            return NotFound();
        }

        var table = await _context.BusinessTables
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.BusinessId == businessId);

        if (table is null)
        {
            return NotFound();
        }

        var publicMenuUrl = BusinessTableCodeHelper.BuildTableMenuUrl(
            $"{Request.Scheme}://{Request.Host}",
            business.Slug,
            table.PublicCode);

        var model = new BusinessTableQrViewModel
        {
            Id = table.Id,
            BusinessName = business.Name,
            TableLabel = table.TableLabel,
            PublicCode = table.PublicCode,
            PublicMenuUrl = publicMenuUrl,
            QrPayload = publicMenuUrl,
            ThemeColor = ResolveThemeColor(business.Setting?.ThemeColor),
            LogoUrl = business.LogoUrl
        };

        return View(model);
    }

    private static string ResolveThemeColor(string? themeColor)
    {
        const string defaultColor = "#2563eb";
        if (string.IsNullOrWhiteSpace(themeColor))
        {
            return defaultColor;
        }

        return Regex.IsMatch(themeColor.Trim(), @"^#[0-9A-Fa-f]{6}$")
            ? themeColor.Trim()
            : defaultColor;
    }
}
