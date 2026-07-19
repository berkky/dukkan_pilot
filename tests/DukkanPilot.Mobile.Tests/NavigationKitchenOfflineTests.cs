using DukkanPilot.Mobile.Core.Navigation;
using DukkanPilot.Mobile.Core.State;

namespace DukkanPilot.Mobile.Tests;

public sealed class NavigationKitchenOfflineTests
{
    [Fact]
    public void ProtectedRouteRedirectsUnauthenticatedUserToLogin()
    {
        var session = new SessionState();
        session.BeginRestore();
        session.CompleteRestore();

        var redirect = AuthRouteGuard.GetRedirect("/orders/42", session);

        Assert.Equal("/login", redirect);
    }

    [Fact]
    public void AuthenticatedUserDoesNotRemainOnLogin()
    {
        var session = new SessionState();
        session.ApplyAuthentication(TestData.Auth());

        var redirect = AuthRouteGuard.GetRedirect("/login", session);

        Assert.Equal("/dashboard", redirect);
    }

    [Fact]
    public void ProtectedContentIsHiddenUntilSessionRestoreCompletes()
    {
        var session = new SessionState();

        var redirect = AuthRouteGuard.GetRedirect("/dashboard", session);

        Assert.Equal("/", redirect);
    }

    [Fact]
    public async Task KitchenPollingStopsAfterDispose()
    {
        var api = new StubMobileApiClient();
        var state = new KitchenState(api, new FakeConnectivityService());
        var polling = new KitchenPollingService(state, TimeSpan.FromMilliseconds(10));
        polling.Start();
        var timeout = DateTime.UtcNow.AddSeconds(2);
        while (api.KitchenCalls < 2 && DateTime.UtcNow < timeout)
        {
            await Task.Delay(5);
        }

        await polling.DisposeAsync();
        var callsAfterDispose = api.KitchenCalls;
        await Task.Delay(40);

        Assert.True(callsAfterDispose >= 2);
        Assert.Equal(callsAfterDispose, api.KitchenCalls);
        Assert.False(polling.IsRunning);
    }

    [Fact]
    public async Task OfflineWriteIsNotSentAndReturnsClearError()
    {
        var api = new StubMobileApiClient();
        var connectivity = new FakeConnectivityService();
        connectivity.SetOnline(false);
        var state = new OrderState(api, connectivity);

        var succeeded = await state.UpdateStatusAsync(1, "Preparing");

        Assert.False(succeeded);
        Assert.Equal(0, api.UpdateStatusCalls);
        Assert.Equal("İnternet bağlantısı yok. İşlem gönderilmedi.", state.LastError);
    }
}
