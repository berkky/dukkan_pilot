using DukkanPilot.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DukkanPilot.Web.Controllers;

[AllowAnonymous]
public class DemoPacksController : Controller
{
    [HttpGet("/DemoPacks")]
    [HttpGet("/Demo/Packs")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Demo Paketleri";
        ViewData["ActiveNav"] = "demo";
        ViewData["MetaDescription"] = "Sektörünüze uygun demo menüyü seçin: kafe, tatlıcı, burgerci, restoran ve lounge örnekleri.";

        var model = DemoPackHelper.BuildPublicPage();
        return View(model);
    }
}
