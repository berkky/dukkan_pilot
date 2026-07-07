using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/QrMenu")]
[RequireActiveSubscription]
public class QrMenuController : BusinessBaseController
{
    private readonly AppDbContext _context;

    public QrMenuController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "qr-menu";

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

        var publicMenuUrl = $"{Request.Scheme}://{Request.Host}/m/{business.Slug}";

        var model = new QrMenuViewModel
        {
            BusinessName = business.Name,
            Slug = business.Slug,
            PublicMenuUrl = publicMenuUrl,
            ThemeColor = business.Setting?.ThemeColor ?? "#2563eb",
            LogoUrl = business.LogoUrl
        };

        return View(model);
    }
}
