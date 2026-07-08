using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Support")]
public class SupportController : BusinessBaseController
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
        string? status,
        string? category,
        string? priority,
        string? search,
        CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "support";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var summary = await _supportTickets.GetBusinessTicketSummaryAsync(businessId, cancellationToken);

        var query = _context.SupportTickets.AsNoTracking()
            .Where(t => t.BusinessId == businessId);

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

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t =>
                t.TicketNumber.Contains(term) || t.Subject.Contains(term));
        }

        var items = await query
            .OrderByDescending(t => t.LastMessageAtUtc ?? t.CreatedAtUtc)
            .Take(200)
            .Select(t => new BusinessSupportTicketRowViewModel
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                Subject = t.Subject,
                Category = t.Category,
                Priority = t.Priority,
                Status = t.Status,
                CreatedAtUtc = t.CreatedAtUtc,
                LastMessageAtUtc = t.LastMessageAtUtc,
                LastMessageByRole = t.LastMessageByRole
            })
            .ToListAsync(cancellationToken);

        var model = new BusinessSupportListViewModel
        {
            Summary = new BusinessSupportSummaryViewModel
            {
                OpenCount = summary.OpenCount,
                WaitingCustomerCount = summary.WaitingCustomerCount,
                ResolvedOrClosedCount = summary.ResolvedOrClosedCount,
                UrgentOrHighCount = summary.UrgentOrHighCount
            },
            Status = status,
            Category = category,
            Priority = priority,
            Search = search,
            Items = items
        };

        return View(model);
    }

    [HttpGet("Create")]
    public IActionResult Create(string? category)
    {
        ViewData["ActiveMenu"] = "support";

        var forbidResult = GetCurrentBusinessIdOrForbid(out _);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var normalizedCategory = string.Equals(category, "FeatureRequest", StringComparison.OrdinalIgnoreCase)
            ? "FeatureRequest"
            : "Other";

        var model = new BusinessSupportCreateViewModel
        {
            Category = SupportTicketDisplayHelper.IsAllowedCategory(normalizedCategory) ? normalizedCategory : "Other",
            IsFeatureRequest = normalizedCategory == "FeatureRequest",
            CategoryOptions = BuildCategoryOptions(),
            PriorityOptions = BuildPriorityOptions(),
            RelatedScreenOptions = BuildRelatedScreenOptions()
        };

        return View(model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BusinessSupportCreateViewModel model, CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "support";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        model.CategoryOptions = BuildCategoryOptions();
        model.PriorityOptions = BuildPriorityOptions();
        model.RelatedScreenOptions = BuildRelatedScreenOptions();
        model.IsFeatureRequest = model.Category == "FeatureRequest";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var source = model.Category == "FeatureRequest" ? "Feedback" : "BusinessPanel";

        var ticket = await _supportTickets.CreateTicketAsync(new SupportTicketCreateInput
        {
            BusinessId = businessId,
            CreatedByUserId = CurrentUserId,
            CreatedByUserEmail = CurrentUserEmail,
            CreatedByUserName = User.Identity?.Name,
            Category = model.Category,
            Priority = model.Priority,
            Subject = model.Subject,
            Message = model.Message,
            Source = source,
            RelatedEntityName = model.RelatedEntityName
        }, cancellationToken);

        if (ticket is null)
        {
            ModelState.AddModelError(string.Empty, "Destek talebi oluşturulamadı.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Destek talebiniz oluşturuldu: {ticket.TicketNumber}";
        return RedirectToAction(nameof(Details), new { id = ticket.Id });
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "support";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var ticket = await _supportTickets.GetTicketForBusinessAsync(id, businessId, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        var model = MapDetails(ticket);
        return View(model);
    }

    [HttpPost("AddMessage/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMessage(int id, BusinessSupportReplyFormViewModel form, CancellationToken cancellationToken)
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        if (string.IsNullOrWhiteSpace(form.Message))
        {
            TempData["ErrorMessage"] = "Mesaj boş olamaz.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var message = await _supportTickets.AddBusinessMessageAsync(new SupportTicketMessageInput
        {
            TicketId = id,
            BusinessId = businessId,
            SenderUserId = CurrentUserId,
            SenderEmail = CurrentUserEmail,
            SenderName = User.Identity?.Name,
            SenderRole = "Business",
            Message = form.Message
        }, cancellationToken);

        TempData[message is null ? "ErrorMessage" : "SuccessMessage"] =
            message is null ? "Mesaj eklenemedi." : "Mesajınız gönderildi.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("Close/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id, CancellationToken cancellationToken)
    {
        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var ticket = await _supportTickets.CloseByBusinessAsync(id, businessId, cancellationToken: cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Destek talebi kapatıldı.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private static BusinessSupportDetailsViewModel MapDetails(DukkanPilot.Core.Entities.SupportTicket ticket) =>
        new()
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Subject = ticket.Subject,
            Category = ticket.Category,
            Priority = ticket.Priority,
            Status = ticket.Status,
            Source = ticket.Source,
            RelatedEntityName = ticket.RelatedEntityName,
            CreatedAtUtc = ticket.CreatedAtUtc,
            ClosedAtUtc = ticket.ClosedAtUtc,
            ResolutionSummary = ticket.ResolutionSummary,
            CanReply = SupportTicketDisplayHelper.IsOpenStatus(ticket.Status),
            Messages = ticket.Messages.Select(m => new BusinessSupportMessageViewModel
            {
                Id = m.Id,
                CreatedAtUtc = m.CreatedAtUtc,
                SenderName = m.SenderName,
                SenderRole = m.SenderRole,
                Message = m.Message,
                IsSystemMessage = m.IsSystemMessage
            }).ToList(),
            ReplyForm = new BusinessSupportReplyFormViewModel { TicketId = ticket.Id }
        };

    private static List<SelectListItem> BuildCategoryOptions() =>
        SupportTicketDisplayHelper.AllowedCategories
            .Select(c => new SelectListItem(SupportTicketDisplayHelper.GetCategoryLabel(c), c))
            .ToList();

    private static List<SelectListItem> BuildPriorityOptions() =>
        SupportTicketDisplayHelper.AllowedPriorities
            .Select(p => new SelectListItem(SupportTicketDisplayHelper.GetPriorityLabel(p), p))
            .ToList();

    private static List<SelectListItem> BuildRelatedScreenOptions() =>
        SupportTicketDisplayHelper.RelatedScreens
            .Select(s => new SelectListItem(SupportTicketDisplayHelper.GetRelatedScreenLabel(s), s))
            .ToList();
}
