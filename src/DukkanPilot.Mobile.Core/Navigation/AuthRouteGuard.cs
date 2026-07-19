using DukkanPilot.Mobile.Core.State;

namespace DukkanPilot.Mobile.Core.Navigation;

public static class AuthRouteGuard
{
    private static readonly string[] ProtectedRoutes =
        ["/dashboard", "/orders", "/kitchen", "/account"];

    public static string? GetRedirect(string path, SessionState session)
    {
        var normalized = Normalize(path);
        if (!session.IsRestoreComplete &&
            ProtectedRoutes.Any(route => normalized.StartsWith(route, StringComparison.OrdinalIgnoreCase)))
        {
            return "/";
        }

        if (!session.IsAuthenticated &&
            ProtectedRoutes.Any(route => normalized.StartsWith(route, StringComparison.OrdinalIgnoreCase)))
        {
            return "/login";
        }

        if (session.IsAuthenticated &&
            (normalized == "/login" || normalized == "/business-select" || normalized == "/"))
        {
            return "/dashboard";
        }

        return null;
    }

    private static string Normalize(string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            path = uri.AbsolutePath;
        }

        return string.IsNullOrWhiteSpace(path) ? "/" : path;
    }
}
