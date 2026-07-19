using DukkanPilot.Web.Api.Mobile.V1.Common;

namespace DukkanPilot.Web.Middleware;

public sealed class MobileApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MobileApiExceptionMiddleware> _logger;

    public MobileApiExceptionMiddleware(
        RequestDelegate next,
        ILogger<MobileApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception) when (
            context.Request.Path.StartsWithSegments("/api/mobile") &&
            !context.Response.HasStarted)
        {
            _logger.LogError(exception, "Unhandled mobile API error. TraceId: {TraceId}", context.TraceIdentifier);
            context.Response.Clear();
            await MobileProblemDetails.WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "internal_error",
                "An unexpected error occurred.");
        }
    }
}
