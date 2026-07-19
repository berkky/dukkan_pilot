namespace DukkanPilot.Mobile.Core.Diagnostics;

public static class SafeRequestLogFormatter
{
    public static string Format(HttpRequestMessage request)
    {
        var path = request.RequestUri?.AbsolutePath ?? "/";
        return $"{request.Method.Method} {path}";
    }
}
