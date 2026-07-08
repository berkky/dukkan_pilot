using DukkanPilot.Core.Enums;
using DukkanPilot.Web.Models.Landing;
using DukkanPilot.Web.Models.Legal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace DukkanPilot.Web.Controllers;

[AllowAnonymous]
public class LegalController : Controller
{
    private readonly IConfiguration _configuration;

    public LegalController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("/Legal/Privacy")]
    [HttpGet("/Privacy")]
    public IActionResult Privacy()
    {
        ViewData["Title"] = "Gizlilik Politikası";
        ViewData["ActiveNav"] = "legal";
        ViewData["MetaDescription"] = "DükkanPilot gizlilik politikası taslağı — kişisel verilerin işlenmesine ilişkin bilgilendirme.";
        return View(CreateModel(
            "Gizlilik Politikası",
            "Kişisel verilerin işlenmesine ilişkin örnek bilgilendirme taslağı."));
    }

    [HttpGet("/Legal/Terms")]
    [HttpGet("/Terms")]
    public IActionResult Terms()
    {
        ViewData["Title"] = "Kullanım Şartları";
        ViewData["ActiveNav"] = "legal";
        ViewData["MetaDescription"] = "DükkanPilot kullanım şartları taslağı.";
        return View(CreateModel(
            "Kullanım Şartları",
            "Hizmetin kullanımına ilişkin örnek şartlar taslağı."));
    }

    [HttpGet("/Legal/Kvkk")]
    [HttpGet("/Kvkk")]
    public IActionResult Kvkk()
    {
        ViewData["Title"] = "KVKK Aydınlatma Metni";
        ViewData["ActiveNav"] = "legal";
        ViewData["MetaDescription"] = "DükkanPilot KVKK aydınlatma metni taslağı.";
        return View(CreateModel(
            "KVKK Aydınlatma Metni Taslağı",
            "6698 sayılı KVKK kapsamında örnek aydınlatma metni taslağı."));
    }

    [HttpGet("/Legal/Cookies")]
    [HttpGet("/Cookies")]
    public IActionResult Cookies()
    {
        ViewData["Title"] = "Çerez Politikası";
        ViewData["ActiveNav"] = "legal";
        ViewData["MetaDescription"] = "DükkanPilot çerez politikası taslağı.";
        return View(CreateModel(
            "Çerez Politikası",
            "Oturum ve güvenlik çerezlerine ilişkin örnek bilgilendirme taslağı."));
    }

    [HttpGet("/Legal/DataProcessing")]
    [HttpGet("/DataProcessing")]
    public IActionResult DataProcessing()
    {
        ViewData["Title"] = "Veri İşleme ve Güvenlik";
        ViewData["ActiveNav"] = "legal";
        ViewData["MetaDescription"] = "DükkanPilot veri işleme ve güvenlik yaklaşımı.";
        return View(CreateModel(
            "Veri İşleme ve Güvenlik Yaklaşımı",
            "SaaS ortamında tenant izolasyonu, erişim ve operasyon güvenliği hakkında şeffaf özet."));
    }

    [HttpGet("/Trust")]
    public IActionResult Trust()
    {
        ViewData["Title"] = "Güven Merkezi";
        ViewData["ActiveNav"] = "trust";
        ViewData["MetaDescription"] =
            "DükkanPilot güven merkezi: erişim kontrolü, audit, yedekleme ve yasal dokümanlara genel bakış.";

        var model = new TrustPageViewModel
        {
            Title = "Güven Merkezi",
            Subtitle = "Operasyon, erişim kontrolü ve şeffaflık odaklı güven yaklaşımımız.",
            AuthCta = BuildAuthCta()
        };
        ApplyAppSettings(model);
        return View(model);
    }

    private LegalPageViewModel CreateModel(string title, string subtitle)
    {
        var model = new LegalPageViewModel
        {
            Title = title,
            Subtitle = subtitle
        };
        ApplyAppSettings(model);
        return model;
    }

    private void ApplyAppSettings(LegalPageViewModel model)
    {
        model.CompanyName = _configuration["App:CompanyName"] ?? model.CompanyName;
        model.SupportEmail = _configuration["App:SupportEmail"] ?? model.SupportEmail;
        model.PublicBaseUrl = _configuration["App:PublicBaseUrl"] ?? model.PublicBaseUrl;
        var lastUpdated = _configuration["App:LegalLastUpdated"];
        if (!string.IsNullOrWhiteSpace(lastUpdated))
        {
            model.LastUpdatedDisplay = lastUpdated;
        }
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
                SecondaryText = "Operasyon Merkezi",
                SecondaryUrl = "/Admin/Operations"
            };
        }

        if (User.IsInRole(nameof(UserRole.BusinessOwner)) || User.IsInRole(nameof(UserRole.Staff)))
        {
            return new LandingAuthCtaViewModel
            {
                IsAuthenticated = true,
                PrimaryText = "Panele Git",
                PrimaryUrl = "/Business/Dashboard",
                SecondaryText = "Demo Merkezi",
                SecondaryUrl = "/Business/DemoCenter"
            };
        }

        return new LandingAuthCtaViewModel();
    }
}
