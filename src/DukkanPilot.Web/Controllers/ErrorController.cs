using System.Diagnostics;
using DukkanPilot.Core.Enums;
using DukkanPilot.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DukkanPilot.Web.Controllers;

[AllowAnonymous]
[Route("Error")]
public class ErrorController : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View("Status500", BuildModel(500));
    }

    [HttpGet("{code:int}")]
    [HttpPost("{code:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult ByStatus(int code)
    {
        var status = code switch
        {
            401 => 401,
            403 => 403,
            404 => 404,
            >= 500 => 500,
            _ => code
        };

        Response.StatusCode = status;

        return status switch
        {
            404 => View("Status404", BuildModel(404)),
            403 => View("Status403", BuildModel(403)),
            401 => View("Status401", BuildModel(401)),
            _ => View("Status500", BuildModel(500))
        };
    }

    [HttpGet("404")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult NotFoundPage()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("Status404", BuildModel(404));
    }

    [HttpGet("500")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult ServerError()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View("Status500", BuildModel(500));
    }

    private ErrorPageViewModel BuildModel(int statusCode)
    {
        return new ErrorPageViewModel
        {
            StatusCode = statusCode,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            IsAuthenticated = User.Identity?.IsAuthenticated == true,
            IsSuperAdmin = User.IsInRole(nameof(UserRole.SuperAdmin)),
            IsBusinessUser = User.IsInRole(nameof(UserRole.BusinessOwner)) || User.IsInRole(nameof(UserRole.Staff))
        };
    }
}
