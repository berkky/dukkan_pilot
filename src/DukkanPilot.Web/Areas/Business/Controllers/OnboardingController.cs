using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/Onboarding")]
public class OnboardingController : BusinessBaseController
{
    private readonly AppDbContext _context;
    private readonly CustomerOnboardingHelper _onboardingHelper;

    public OnboardingController(AppDbContext context, CustomerOnboardingHelper onboardingHelper)
    {
        _context = context;
        _onboardingHelper = onboardingHelper;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "onboarding";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var exists = await _context.Businesses.AsNoTracking()
            .AnyAsync(b => b.Id == businessId);
        if (!exists)
        {
            return NotFound();
        }

        var slug = await _context.Businesses.AsNoTracking()
            .Where(b => b.Id == businessId)
            .Select(b => b.Slug)
            .FirstAsync();

        var publicMenuUrl = $"{Request.Scheme}://{Request.Host}/m/{slug}";
        var isOwner = User.IsInRole(nameof(UserRole.BusinessOwner));
        var snapshot = await _onboardingHelper.BuildAsync(businessId, publicMenuUrl, isOwner);
        if (snapshot is null)
        {
            return NotFound();
        }

        return View(new BusinessOnboardingViewModel
        {
            Snapshot = snapshot,
            IsBusinessOwner = isOwner
        });
    }
}
