using DukkanPilot.Web.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Route("Admin/HelpCenter")]
public class HelpCenterController : AdminBaseController
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Yardım Merkezi";
        ViewData["ActiveMenu"] = "help-center";

        var model = HelpContentHelper.BuildIndex(
            HelpContentHelper.ScopeAdmin,
            "/Admin/HelpCenter/Article",
            "/Admin/HelpCenter");

        return View(model);
    }

    [HttpGet("Article/{slug}")]
    public IActionResult Article(string slug)
    {
        var model = HelpContentHelper.BuildDetail(
            slug,
            HelpContentHelper.ScopeAdmin,
            "/Admin/HelpCenter/Article",
            "/Admin/HelpCenter");

        if (string.IsNullOrWhiteSpace(model.Article.Slug))
        {
            return NotFound();
        }

        ViewData["Title"] = model.Article.Title;
        ViewData["ActiveMenu"] = "help-center";

        return View(model);
    }
}
