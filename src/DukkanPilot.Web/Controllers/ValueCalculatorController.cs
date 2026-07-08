using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Models.Value;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DukkanPilot.Web.Controllers;

[AllowAnonymous]
public class ValueCalculatorController : Controller
{
    [HttpGet("/RoiCalculator")]
    [HttpGet("/ValueCalculator")]
    public IActionResult Index(
        [FromQuery] decimal? monthlySoftwareCost,
        [FromQuery] int? monthlyOrders,
        [FromQuery] decimal? averageBasket)
    {
        ViewData["Title"] = "Değer Hesaplayıcı";
        ViewData["MetaDescription"] = "DukkanPilot ile tahmini değer senaryosu hesaplayın — tekrar sipariş, zaman tasarrufu ve kampanya etkisi.";
        ViewData["ActiveNav"] = "roi";

        var input = ValueCalculatorHelper.CreatePublicDefaults(monthlySoftwareCost);

        if (monthlyOrders.HasValue)
        {
            input.MonthlyOrders = monthlyOrders.Value;
        }

        if (averageBasket.HasValue)
        {
            input.AverageBasket = averageBasket.Value;
        }

        var model = new ValueCalculatorPageViewModel
        {
            PageTitle = "DukkanPilot işletmenize ne kadar değer katabilir?",
            Intro = "QR menü, WhatsApp sipariş, kampanya ve sadakat ile tahmini değer senaryosu oluşturun. " +
                    "Varsayımlarınızı değiştirerek farklı sonuçları karşılaştırabilirsiniz.",
            FormAction = "/RoiCalculator",
            Input = ValueCalculatorHelper.SanitizeInput(input),
            Context = "Public"
        };

        return View(model);
    }

    [HttpPost("/RoiCalculator")]
    [HttpPost("/ValueCalculator")]
    [ValidateAntiForgeryToken]
    public IActionResult Index(ValueCalculatorInputViewModel input)
    {
        ViewData["Title"] = "Değer Hesaplayıcı";
        ViewData["MetaDescription"] = "DukkanPilot ile tahmini değer senaryosu hesaplayın.";
        ViewData["ActiveNav"] = "roi";

        var sanitized = ValueCalculatorHelper.SanitizeInput(input);
        var result = ValueCalculatorHelper.Calculate(sanitized, "Public");

        var model = new ValueCalculatorPageViewModel
        {
            PageTitle = "DukkanPilot işletmenize ne kadar değer katabilir?",
            Intro = "QR menü, WhatsApp sipariş, kampanya ve sadakat ile tahmini değer senaryosu oluşturun.",
            FormAction = "/RoiCalculator",
            Input = sanitized,
            Result = result,
            ShowResult = true,
            Context = "Public"
        };

        return View(model);
    }
}
