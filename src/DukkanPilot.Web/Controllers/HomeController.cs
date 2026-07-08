using System.Diagnostics;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Models;
using DukkanPilot.Web.Models.Landing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;
    private readonly BusinessPlanLimitHelper _planLimitHelper;

    public HomeController(AppDbContext context, BusinessPlanLimitHelper planLimitHelper)
    {
        _context = context;
        _planLimitHelper = planLimitHelper;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveNav"] = "home";
        ViewData["MetaDescription"] =
            "DükkanPilot ile QR menü, WhatsApp sipariş, sadakat, kampanya ve müşteri CRM’ini tek panelden yönetin.";

        var (plans, fromDb) = await LoadPlansAsync();
        var model = new LandingPageViewModel
        {
            AuthCta = BuildAuthCta(),
            DemoMenuUrl = "/m/demo-kafe",
            Plans = plans.Take(3).ToList(),
            PlansFromDatabase = fromDb
        };

        return View(model);
    }

    [HttpGet("/Pricing")]
    public async Task<IActionResult> Pricing()
    {
        ViewData["Title"] = "Fiyatlar";
        ViewData["ActiveNav"] = "pricing";
        ViewData["MetaDescription"] =
            "DükkanPilot abonelik planları: ürün, kampanya, personel ve QR limitlerini karşılaştırın.";

        var (plans, fromDb) = await LoadPlansAsync();
        var model = new PricingPageViewModel
        {
            AuthCta = BuildAuthCta(),
            Plans = plans,
            PlansFromDatabase = fromDb
        };

        return View(model);
    }

    [HttpGet("/Features")]
    public IActionResult Features()
    {
        ViewData["Title"] = "Özellikler";
        ViewData["ActiveNav"] = "features";
        ViewData["MetaDescription"] =
            "QR menü, WhatsApp sipariş, mutfak modu, kampanya motoru, sadakat, CRM, raporlama ve Go-Live Merkezi.";

        ViewBag.AuthCta = BuildAuthCta();
        return View();
    }

    [HttpGet("/Demo")]
    public IActionResult Demo()
    {
        ViewData["Title"] = "Demo";
        ViewData["ActiveNav"] = "demo";
        ViewData["MetaDescription"] =
            "Demo Kafe QR menüsünü açın; sepet, kampanya indirimi, sipariş ve takip akışını deneyin.";

        var model = new DemoPageViewModel
        {
            AuthCta = BuildAuthCta(),
            DemoBusinessName = "Demo Kafe",
            DemoSlug = "demo-kafe",
            DemoMenuUrl = "/m/demo-kafe"
        };

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task<(List<LandingPlanCardViewModel> Plans, bool FromDatabase)> LoadPlansAsync()
    {
        var activePlans = await _context.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Price)
            .ToListAsync();

        if (activePlans.Count == 0)
        {
            return (LandingPlanMapper.FallbackPlans(), false);
        }

        var cards = activePlans
            .Select(p => LandingPlanMapper.FromAvailablePlan(_planLimitHelper.MapToAvailablePlan(p, null)))
            .ToList();

        return (cards, true);
    }

    private LandingAuthCtaViewModel BuildAuthCta()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return new LandingAuthCtaViewModel();
        }

        if (User.IsInRole(nameof(UserRole.SuperAdmin)))
        {
            return new LandingAuthCtaViewModel
            {
                IsAuthenticated = true,
                PrimaryText = "Admin Paneli",
                PrimaryUrl = "/Admin/Dashboard",
                SecondaryText = "Çıkış / Giriş",
                SecondaryUrl = "/Account/Login"
            };
        }

        if (User.IsInRole(nameof(UserRole.BusinessOwner)) || User.IsInRole(nameof(UserRole.Staff)))
        {
            return new LandingAuthCtaViewModel
            {
                IsAuthenticated = true,
                PrimaryText = "Panele Git",
                PrimaryUrl = "/Business/Dashboard",
                SecondaryText = "Go-Live Merkezi",
                SecondaryUrl = "/Business/GoLive"
            };
        }

        return new LandingAuthCtaViewModel
        {
            IsAuthenticated = true,
            PrimaryText = "Panele Git",
            PrimaryUrl = "/Business/Dashboard",
            SecondaryText = "Giriş Yap",
            SecondaryUrl = "/Account/Login"
        };
    }
}
