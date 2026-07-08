using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Models.Help;
using Microsoft.AspNetCore.Mvc;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/HelpCenter")]
public class HelpCenterController : BusinessBaseController
{
    [HttpGet("")]
    public IActionResult Index()
    {
        if (GetCurrentBusinessIdOrForbid(out _) is not null)
        {
            return Forbid();
        }

        ViewData["Title"] = "Yardım Merkezi";
        ViewData["ActiveMenu"] = "help-center";

        var model = HelpContentHelper.BuildIndex(
            HelpContentHelper.ScopeBusiness,
            "/Business/HelpCenter/Article",
            "/Business/HelpCenter");

        return View(model);
    }

    [HttpGet("Article/{slug}")]
    public IActionResult Article(string slug)
    {
        if (GetCurrentBusinessIdOrForbid(out _) is not null)
        {
            return Forbid();
        }

        var model = HelpContentHelper.BuildDetail(
            slug,
            HelpContentHelper.ScopeBusiness,
            "/Business/HelpCenter/Article",
            "/Business/HelpCenter");

        if (string.IsNullOrWhiteSpace(model.Article.Slug))
        {
            return NotFound();
        }

        ViewData["Title"] = model.Article.Title;
        ViewData["ActiveMenu"] = "help-center";

        return View(model);
    }
}
