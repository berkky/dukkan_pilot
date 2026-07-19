using Microsoft.AspNetCore.Mvc;

namespace DukkanPilot.Web.Api.Mobile.V1.Common;

public static class MobileProblemDetails
{
    public static ProblemDetails Create(
        HttpContext context,
        int status,
        string code,
        string title,
        string? detail = null,
        IReadOnlyDictionary<string, object?>? extensions = null)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{status}",
            Instance = context.Request.Path
        };

        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (extensions is not null)
        {
            foreach (var extension in extensions)
            {
                problem.Extensions[extension.Key] = extension.Value;
            }
        }

        return problem;
    }

    public static async Task WriteAsync(
        HttpContext context,
        int status,
        string code,
        string title,
        string? detail = null)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await System.Text.Json.JsonSerializer.SerializeAsync(
            context.Response.Body,
            Create(context, status, code, title, detail),
            cancellationToken: context.RequestAborted);
    }
}

[ApiController]
public abstract class MobileApiControllerBase : ControllerBase
{
    protected ObjectResult MobileProblem(
        int status,
        string code,
        string title,
        string? detail = null,
        IReadOnlyDictionary<string, object?>? extensions = null)
    {
        return new ObjectResult(MobileProblemDetails.Create(HttpContext, status, code, title, detail, extensions))
        {
            StatusCode = status
        };
    }
}
