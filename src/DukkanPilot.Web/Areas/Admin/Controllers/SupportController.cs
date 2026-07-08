using System.Security.Claims;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Admin.Models;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Route("Admin/Support")]
public class SupportController : AdminBaseController
{
    private readonly AppDbContext _context;
    private readonly ISupportTicketService _supportTickets;

    public SupportController(AppDbContext context, ISupportTicketService supportTickets)
    {
        _context = context;
        _supportTickets = supportTickets;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        int? businessId,
        string? status,
        string? category,
        string? priority,
        string? assigned,
        string? search,
        CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "support";

        var summary = await _supportTickets.GetAdminTicketSummaryAsync(cancellationToken);
        var businesses = await _context.Businesses.AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new { b.Id, b.Name })
            .ToListAsync(cancellationToken);

        var query = _context.SupportTickets.AsNoTracking().AsQueryable();

        if (businessId is int bid)
        {
            query = query.Where(t => t.BusinessId == bid);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(t => t.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(t => t.Priority == priority);
        }

        if (string.Equals(assigned, "unassigned", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t => t.AssignedAdminUserId == null);
        }
        else if (string.Equals(assigned, "assigned", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t => t.AssignedAdminUserId != null);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var matchingBusinessIds = businesses
                .Where(b => b.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(b => b.Id)
                .ToList();

            query = query.Where(t =>
                t.TicketNumber.Contains(term)
                || t.Subject.Contains(term)
                || matchingBusinessIds.Contains(t.BusinessId));
        }

        var rows = await query
            .OrderByDescending(t => t.LastMessageAtUtc ?? t.CreatedAtUtc)
            .Take(300)
            .ToListAsync(cancellationToken);

        var businessMap = businesses.ToDictionary(b => b.Id, b => b.Name);

        var items = rows.Select(t => new AdminSupportTicketRowViewModel
        {
            Id = t.Id,
            TicketNumber = t.TicketNumber,
            BusinessId = t.BusinessId,
            BusinessName = businessMap.GetValueOrDefault(t.BusinessId, $"#{t.BusinessId}"),
            Subject = t.Subject,
            Category = t.Category,
            Priority = t.Priority,
            Status = t.Status,
            CreatedAtUtc = t.CreatedAtUtc,
            LastMessageAtUtc = t.LastMessageAtUtc,
            LastMessageByRole = t.LastMessageByRole,
            AssignedAdminEmail = t.AssignedAdminEmail
        }).ToList();

        var now = DateTime.UtcNow;
        var attention = rows
            .Where(t => SupportTicketDisplayHelper.IsOpenStatus(t.Status))
            .Select(t => new AdminSupportAttentionRowViewModel
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                BusinessName = businessMap.GetValueOrDefault(t.BusinessId, $"#{t.BusinessId}"),
                Subject = t.Subject,
                Priority = t.Priority,
                Status = t.Status,
                CreatedAtUtc = t.CreatedAtUtc,
                Reason = t.Priority is "Urgent" or "High"
                    ? "Yüksek öncelik"
                    : (now - t.CreatedAtUtc).TotalDays >= 3
                        ? "3+ gün açık"
                        : "Takip gerekli"
            })
            .Where(a => a.Reason != "Takip gerekli" || a.Priority is "Urgent" or "High")
            .OrderByDescending(a => a.Priority == "Urgent")
            .ThenByDescending(a => a.Priority == "High")
            .Take(10)
            .ToList();

        var model = new AdminSupportListViewModel
        {
            Summary = new AdminSupportSummaryViewModel
            {
                NewCount = summary.NewCount,
                OpenOrInProgressCount = summary.OpenOrInProgressCount,
                WaitingCustomerCount = summary.WaitingCustomerCount,
                UrgentOrHighCount = summary.UrgentOrHighCount,
                ResolvedThisMonthCount = summary.ResolvedThisMonthCount,
                WaitingAdminCount = summary.WaitingAdminCount
            },
            BusinessId = businessId,
            Status = status,
            Category = category,
            Priority = priority,
            Assigned = assigned,
            Search = search,
            Items = items,
            AttentionItems = attention,
            BusinessOptions = businesses
                .Select(b => new SelectListItem(b.Name, b.Id.ToString(), businessId == b.Id))
                .Prepend(new SelectListItem("Tüm işletmeler", ""))
                .ToList()
        };

        return View(model);
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "support";

        var ticket = await _supportTickets.GetTicketForAdminAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        var business = await _context.Businesses.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == ticket.BusinessId, cancellationToken);

        var model = new AdminSupportDetailsViewModel
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            BusinessId = ticket.BusinessId,
            BusinessName = business?.Name ?? $"#{ticket.BusinessId}",
            BusinessSlug = business?.Slug ?? string.Empty,
            Subject = ticket.Subject,
            Category = ticket.Category,
            Priority = ticket.Priority,
            Status = ticket.Status,
            Source = ticket.Source,
            CreatedByUserName = ticket.CreatedByUserName,
            CreatedByUserEmail = ticket.CreatedByUserEmail,
            AssignedAdminEmail = ticket.AssignedAdminEmail,
            RelatedEntityName = ticket.RelatedEntityName,
            ResolutionSummary = ticket.ResolutionSummary,
            AdminInternalNote = ticket.AdminInternalNote,
            CreatedAtUtc = ticket.CreatedAtUtc,
            ClosedAtUtc = ticket.ClosedAtUtc,
            Messages = ticket.Messages.Select(m => new AdminSupportMessageViewModel
            {
                Id = m.Id,
                CreatedAtUtc = m.CreatedAtUtc,
                SenderName = m.SenderName,
                SenderRole = m.SenderRole,
                Message = m.Message,
                IsInternal = m.IsInternal,
                IsSystemMessage = m.IsSystemMessage
            }).ToList(),
            ReplyForm = new AdminSupportReplyFormViewModel { TicketId = ticket.Id },
            InternalNoteForm = new AdminSupportInternalNoteFormViewModel { TicketId = ticket.Id },
            StatusForm = new AdminSupportStatusFormViewModel { TicketId = ticket.Id, Status = ticket.Status },
            PriorityForm = new AdminSupportPriorityFormViewModel { TicketId = ticket.Id, Priority = ticket.Priority },
            AssignForm = new AdminSupportAssignFormViewModel
            {
                TicketId = ticket.Id,
                AssignedAdminEmail = ticket.AssignedAdminEmail ?? User.FindFirstValue(ClaimTypes.Email)
            },
            StatusOptions = SupportTicketDisplayHelper.AllowedStatuses
                .Select(s => new SelectListItem(SupportTicketDisplayHelper.GetStatusLabel(s), s, s == ticket.Status))
                .ToList(),
            PriorityOptions = SupportTicketDisplayHelper.AllowedPriorities
                .Select(p => new SelectListItem(SupportTicketDisplayHelper.GetPriorityLabel(p), p, p == ticket.Priority))
                .ToList()
        };

        return View(model);
    }

    [HttpPost("AddReply/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReply(int id, AdminSupportReplyFormViewModel form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.Message))
        {
            TempData["ErrorMessage"] = "Yanıt boş olamaz.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var message = await _supportTickets.AddAdminReplyAsync(new SupportTicketMessageInput
        {
            TicketId = id,
            BusinessId = 0,
            SenderUserId = TryParseUserId(),
            SenderEmail = User.FindFirstValue(ClaimTypes.Email),
            SenderName = User.Identity?.Name,
            SenderRole = "Admin",
            Message = form.Message,
            IsInternal = false
        }, cancellationToken);

        TempData[message is null ? "ErrorMessage" : "SuccessMessage"] =
            message is null ? "Yanıt eklenemedi." : "Yanıt gönderildi.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("AddInternalNote/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddInternalNote(int id, AdminSupportInternalNoteFormViewModel form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.Message))
        {
            TempData["ErrorMessage"] = "İç not boş olamaz.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var message = await _supportTickets.AddAdminInternalNoteAsync(new SupportTicketMessageInput
        {
            TicketId = id,
            SenderUserId = TryParseUserId(),
            SenderEmail = User.FindFirstValue(ClaimTypes.Email),
            SenderName = User.Identity?.Name,
            SenderRole = "Admin",
            Message = form.Message,
            IsInternal = true
        }, cancellationToken);

        TempData[message is null ? "ErrorMessage" : "SuccessMessage"] =
            message is null ? "İç not eklenemedi." : "İç not kaydedildi.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("UpdateStatus/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, AdminSupportStatusFormViewModel form, CancellationToken cancellationToken)
    {
        var ticket = await _supportTickets.UpdateTicketStatusAsync(new SupportTicketStatusUpdateInput
        {
            TicketId = id,
            Status = form.Status,
            ResolutionSummary = form.ResolutionSummary
        }, cancellationToken);

        TempData[ticket is null ? "ErrorMessage" : "SuccessMessage"] =
            ticket is null ? "Durum güncellenemedi." : "Durum güncellendi.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("UpdatePriority/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePriority(int id, AdminSupportPriorityFormViewModel form, CancellationToken cancellationToken)
    {
        var ticket = await _supportTickets.UpdateTicketPriorityAsync(id, form.Priority, cancellationToken);

        TempData[ticket is null ? "ErrorMessage" : "SuccessMessage"] =
            ticket is null ? "Öncelik güncellenemedi." : "Öncelik güncellendi.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("Assign/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int id, AdminSupportAssignFormViewModel form, CancellationToken cancellationToken)
    {
        var ticket = await _supportTickets.AssignTicketAsync(new SupportTicketAssignInput
        {
            TicketId = id,
            AssignedAdminUserId = TryParseUserId(),
            AssignedAdminEmail = form.AssignedAdminEmail ?? User.FindFirstValue(ClaimTypes.Email)
        }, cancellationToken);

        TempData[ticket is null ? "ErrorMessage" : "SuccessMessage"] =
            ticket is null ? "Atama yapılamadı." : "Talep size atandı.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("Close/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id, string? resolutionSummary, CancellationToken cancellationToken)
    {
        var ticket = await _supportTickets.CloseTicketAsync(id, resolutionSummary, cancellationToken);

        TempData[ticket is null ? "ErrorMessage" : "SuccessMessage"] =
            ticket is null ? "Kapatılamadı." : "Talep kapatıldı.";

        return RedirectToAction(nameof(Details), new { id });
    }

    private int? TryParseUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) && id > 0 ? id : null;
    }
}
