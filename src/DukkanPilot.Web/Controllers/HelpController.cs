using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Models.Help;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DukkanPilot.Web.Controllers;

[AllowAnonymous]
public class HelpController : Controller
{
    [HttpGet("/Help")]
    [HttpGet("/Help/Index")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Yardım Merkezi";
        ViewData["MetaDescription"] = "DükkanPilot yardım merkezi — ürün tanıtımı, demo, QR menü ve başlangıç rehberleri.";
        ViewData["ActiveNav"] = "help";

        var model = HelpContentHelper.BuildIndex(
            HelpContentHelper.ScopePublic,
            "/Help",
            "/Help");

        return View(model);
    }

    [HttpGet("/Help/{slug}")]
    public IActionResult Article(string slug)
    {
        var model = HelpContentHelper.BuildDetail(
            slug,
            HelpContentHelper.ScopePublic,
            "/Help",
            "/Help");

        if (string.IsNullOrWhiteSpace(model.Article.Slug))
        {
            return NotFound();
        }

        ViewData["Title"] = model.Article.Title;
        ViewData["MetaDescription"] = model.Article.Summary;
        ViewData["ActiveNav"] = "help";

        return View(model);
    }
}
