using System.Reflection;

namespace DukkanPilot.Mobile.Services;

public sealed record ApiEndpointConfiguration(
    Uri BaseUri,
    string? ConfigurationError,
    bool IsDebug)
{
    public const string DebugOverridePreferenceKey = "DukkanPilot.ApiBaseUrlOverride";

    public static ApiEndpointConfiguration Resolve()
    {
        var configured = typeof(MauiProgram).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "DukkanPilotApiBaseUrl")
            ?.Value;

#if DEBUG
        var preferenceOverride = Preferences.Default.Get(DebugOverridePreferenceKey, string.Empty);
        var candidate = !string.IsNullOrWhiteSpace(preferenceOverride)
            ? preferenceOverride
            : !string.IsNullOrWhiteSpace(configured)
                ? configured
                : DeviceInfo.Platform == DevicePlatform.Android
                    ? "http://10.0.2.2:5000"
                    : "http://localhost:5000";
        return Create(candidate, isDebug: true);
#else
        if (string.IsNullOrWhiteSpace(configured))
        {
            return new ApiEndpointConfiguration(
                new Uri("https://configuration.invalid/"),
                "Release API adresi yapılandırılmamış. DukkanPilotApiBaseUrl değerini HTTPS adresiyle sağlayın.",
                false);
        }

        return Create(configured, isDebug: false);
#endif
    }

    public static void SetDebugOverride(string? value)
    {
#if DEBUG
        if (string.IsNullOrWhiteSpace(value))
        {
            Preferences.Default.Remove(DebugOverridePreferenceKey);
        }
        else
        {
            Preferences.Default.Set(DebugOverridePreferenceKey, value.Trim());
        }
#endif
    }

    private static ApiEndpointConfiguration Create(string candidate, bool isDebug)
    {
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            return new ApiEndpointConfiguration(
                new Uri("https://configuration.invalid/"),
                "API sunucu adresi geçerli bir HTTP/HTTPS adresi değil.",
                isDebug);
        }

        if (!isDebug && uri.Scheme != Uri.UriSchemeHttps)
        {
            return new ApiEndpointConfiguration(
                new Uri("https://configuration.invalid/"),
                "Release sürümünde API sunucu adresi HTTPS olmalıdır.",
                false);
        }

        return new ApiEndpointConfiguration(
            new Uri(uri.ToString().TrimEnd('/') + "/"),
            null,
            isDebug);
    }
}
