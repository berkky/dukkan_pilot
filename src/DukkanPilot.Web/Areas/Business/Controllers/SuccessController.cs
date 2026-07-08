using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Success")]
public class SuccessController : BusinessBaseController
{
    private readonly AppDbContext _context;
    private readonly CustomerSuccessHealthHelper _successHelper;

    public SuccessController(AppDbContext context, CustomerSuccessHealthHelper successHelper)
    {
        _context = context;
        _successHelper = successHelper;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "success";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var slug = await _context.Businesses.AsNoTracking()
            .Where(b => b.Id == businessId)
            .Select(b => b.Slug)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        var publicMenuUrl = $"{Request.Scheme}://{Request.Host}/m/{slug}";
        var isOwner = User.IsInRole(nameof(UserRole.BusinessOwner));
        var snapshot = await _successHelper.BuildAsync(businessId, publicMenuUrl, isOwner, cancellationToken);
        if (snapshot is null)
        {
            return NotFound();
        }

        return View(new BusinessSuccessViewModel
        {
            Snapshot = snapshot,
            IsBusinessOwner = isOwner
        });
    }
}
