using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DukkanPilot.Web.Controllers;

[AllowAnonymous]
public class SeoController : Controller
{
    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult Robots()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var content = new StringBuilder()
            .AppendLine("User-agent: *")
            .AppendLine("Allow: /")
            .AppendLine("Disallow: /Admin")
            .AppendLine("Disallow: /Business")
            .AppendLine("Disallow: /Account")
            .AppendLine("Disallow: /Error")
            .AppendLine("Disallow: /health")
            .AppendLine("Disallow: /*/order-status")
            .AppendLine("Disallow: /*/order-confirmation")
            .AppendLine($"Sitemap: {baseUrl}/sitemap.xml")
            .ToString();

        return Content(content, "text/plain", Encoding.UTF8);
    }

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult Sitemap()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var urls = new[]
        {
            ("/", "1.0", "daily"),
            ("/Features", "0.8", "weekly"),
            ("/Pricing", "0.8", "weekly"),
            ("/Demo", "0.7", "weekly"),
            ("/Account/Register", "0.6", "monthly"),
            ("/m/demo-kafe", "0.7", "weekly")
        };

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        foreach (var (path, priority, changefreq) in urls)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{baseUrl}{path}</loc>");
            sb.AppendLine($"    <changefreq>{changefreq}</changefreq>");
            sb.AppendLine($"    <priority>{priority}</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");
        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }
}
