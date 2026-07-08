using DukkanPilot.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Controllers;

[AllowAnonymous]
public class HealthController : Controller
{
    private readonly AppDbContext _context;
    private readonly IHostEnvironment _environment;

    public HealthController(AppDbContext context, IHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet("/health")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var databaseStatus = "ok";
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                databaseStatus = "fail";
            }
        }
        catch
        {
            databaseStatus = "fail";
        }

        var payload = new
        {
            status = databaseStatus == "ok" ? "ok" : "degraded",
            app = "DukkanPilot",
            environment = _environment.EnvironmentName,
            database = databaseStatus,
            timeUtc = DateTime.UtcNow.ToString("O")
        };

        if (databaseStatus != "ok")
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, payload);
        }

        return Json(payload);
    }
}
