using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/GoLive")]
public class GoLiveController : BusinessBaseController
{
    private readonly AppDbContext _context;
    private readonly GoLiveHelper _goLiveHelper;

    public GoLiveController(AppDbContext context, GoLiveHelper goLiveHelper)
    {
        _context = context;
        _goLiveHelper = goLiveHelper;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "go-live";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var businessExists = await _context.Businesses
            .AsNoTracking()
            .AnyAsync(b => b.Id == businessId);

        if (!businessExists)
        {
            return NotFound();
        }

        var slug = await _context.Businesses
            .AsNoTracking()
            .Where(b => b.Id == businessId)
            .Select(b => b.Slug)
            .FirstAsync();

        var publicMenuUrl = $"{Request.Scheme}://{Request.Host}/m/{slug}";
        var isBusinessOwner = User.IsInRole(nameof(UserRole.BusinessOwner));

        var model = await _goLiveHelper.BuildAsync(businessId, publicMenuUrl, isBusinessOwner);
        return View(model);
    }
}
