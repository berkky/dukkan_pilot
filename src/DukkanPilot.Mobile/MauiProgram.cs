using Microsoft.Extensions.Logging;
using DukkanPilot.Mobile.Core.Api;
using DukkanPilot.Mobile.Core.Connectivity;
using DukkanPilot.Mobile.Core.Security;
using DukkanPilot.Mobile.Core.Session;
using DukkanPilot.Mobile.Core.State;
using DukkanPilot.Mobile.Services;

namespace DukkanPilot.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
		var endpoint = ApiEndpointConfiguration.Resolve();
		builder.Services.AddSingleton(endpoint);
		builder.Services.AddSingleton<SessionState>();
		builder.Services.AddSingleton<BootstrapState>();
		builder.Services.AddSingleton<ISecureStorage>(_ => SecureStorage.Default);
		builder.Services.AddSingleton<ISecureTokenStore, MauiSecureTokenStore>();
		builder.Services.AddSingleton<IConnectivityService, MauiConnectivityService>();
		builder.Services.AddSingleton<MobileTokenManager>(services =>
		{
			var rawClient = CreateHttpClient(endpoint.BaseUri, new HttpClientHandler());
			var rawApi = new MobileApiClient(rawClient);
			return new MobileTokenManager(
				services.GetRequiredService<SessionState>(),
				services.GetRequiredService<ISecureTokenStore>(),
				rawApi);
		});
		builder.Services.AddSingleton<IMobileApiClient>(services =>
		{
			var handler = new BearerRefreshHandler(
				services.GetRequiredService<SessionState>(),
				services.GetRequiredService<MobileTokenManager>(),
				new HttpClientHandler());
			return new MobileApiClient(CreateHttpClient(endpoint.BaseUri, handler));
		});
		builder.Services.AddSingleton<IMobileSessionService, MobileSessionService>();
		builder.Services.AddSingleton<OrderState>();
		builder.Services.AddSingleton<DashboardState>();
		builder.Services.AddSingleton<KitchenState>();
		builder.Services.AddTransient(services => new KitchenPollingService(
			services.GetRequiredService<KitchenState>()));
		builder.Services.AddSingleton<AppStartupService>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static HttpClient CreateHttpClient(
		Uri baseUri,
		HttpMessageHandler handler)
	{
		return new HttpClient(handler)
		{
			BaseAddress = baseUri,
			Timeout = TimeSpan.FromSeconds(30)
		};
	}
}
