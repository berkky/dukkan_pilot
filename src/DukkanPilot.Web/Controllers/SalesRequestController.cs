using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Models.Sales;
using DukkanPilot.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Controllers;

[AllowAnonymous]
[Route("Sales")]
public class SalesRequestController : Controller
{
    private readonly AppDbContext _context;
    private readonly ISalesRequestService _salesRequests;

    public SalesRequestController(AppDbContext context, ISalesRequestService salesRequests)
    {
        _context = context;
        _salesRequests = salesRequests;
    }

    [HttpGet("RequestDemo")]
    public IActionResult RequestDemo()
    {
        ViewData["Title"] = "Demo Talebi";
        ViewData["ActiveNav"] = "demo";
        ViewData["MetaDescription"] = "DükkanPilot demo görüşmesi veya ürün tanıtımı talebi oluşturun.";
        return View("RequestForm", new PublicSalesRequestFormViewModel { FormMode = "Demo" });
    }

    [HttpPost("RequestDemo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestDemo(PublicSalesRequestFormViewModel model)
    {
        ViewData["Title"] = "Demo Talebi";
        ViewData["ActiveNav"] = "demo";
        model.FormMode = "Demo";
        model.RequestedPlanId = null;
        model.RequestedPlanName = null;

        if (!ModelState.IsValid)
        {
            return View("RequestForm", model);
        }

        var result = await _salesRequests.CreatePublicRequestAsync(new PublicSalesRequestCreateInput
        {
            Source = "PublicDemo",
            RequestType = "DemoRequest",
            ContactName = model.ContactName,
            BusinessName = model.BusinessName,
            Email = model.Email,
            Phone = model.Phone,
            Message = model.Message,
            PrivacyNoticeAcknowledged = model.PrivacyNoticeAcknowledged,
            KvkkNoticeAcknowledged = model.KvkkNoticeAcknowledged
        });

        TempData["SalesRequestDuplicate"] = result.WasDuplicate;
        return RedirectToAction(nameof(ThankYou));
    }

    [HttpGet("RequestPlan")]
    public async Task<IActionResult> RequestPlan(int? planId)
    {
        ViewData["Title"] = "Plan Talebi";
        ViewData["ActiveNav"] = "pricing";
        ViewData["MetaDescription"] = "DükkanPilot abonelik planı için talep oluşturun.";

        var model = new PublicSalesRequestFormViewModel { FormMode = "Plan" };
        if (planId is int id)
        {
            var plan = await _context.SubscriptionPlans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            if (plan is not null)
            {
                model.RequestedPlanId = plan.Id;
                model.RequestedPlanName = plan.Name;
                model.RequestedPlanPrice = plan.Price;
            }
        }

        return View("RequestForm", model);
    }

    [HttpPost("RequestPlan")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestPlan(PublicSalesRequestFormViewModel model)
    {
        ViewData["Title"] = "Plan Talebi";
        ViewData["ActiveNav"] = "pricing";
        model.FormMode = "Plan";

        if (model.RequestedPlanId is int planId)
        {
            var plan = await _context.SubscriptionPlans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == planId && p.IsActive);
            if (plan is null)
            {
                ModelState.AddModelError(nameof(model.RequestedPlanId), "Seçilen plan bulunamadı.");
            }
            else
            {
                model.RequestedPlanName = plan.Name;
                model.RequestedPlanPrice = plan.Price;
            }
        }

        if (!ModelState.IsValid)
        {
            return View("RequestForm", model);
        }

        var result = await _salesRequests.CreatePublicRequestAsync(new PublicSalesRequestCreateInput
        {
            Source = "PublicPricing",
            RequestType = "PlanRequest",
            ContactName = model.ContactName,
            BusinessName = model.BusinessName,
            Email = model.Email,
            Phone = model.Phone,
            Message = model.Message,
            RequestedPlanId = model.RequestedPlanId,
            RequestedPlanName = model.RequestedPlanName,
            PrivacyNoticeAcknowledged = model.PrivacyNoticeAcknowledged,
            KvkkNoticeAcknowledged = model.KvkkNoticeAcknowledged
        });

        TempData["SalesRequestDuplicate"] = result.WasDuplicate;
        return RedirectToAction(nameof(ThankYou));
    }

    [HttpGet("ThankYou")]
    public IActionResult ThankYou()
    {
        ViewData["Title"] = "Talebiniz Alındı";
        ViewData["ActiveNav"] = "demo";
        ViewBag.WasDuplicate = TempData["SalesRequestDuplicate"] as bool? == true;
        return View();
    }
}
