using System.Net;
using DukkanPilot.Mobile.Core.Api;
using DukkanPilot.Mobile.Core.Contracts;
using DukkanPilot.Mobile.Core.Security;
using DukkanPilot.Mobile.Core.Session;
using DukkanPilot.Mobile.Core.State;

namespace DukkanPilot.Mobile.Tests;

public sealed class AuthSessionTests
{
    [Fact]
    public async Task LoginSuccessCreatesTokenAndSessionState()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.LoginAsync(" OWNER@EXAMPLE.TEST ", "secret");

        Assert.Equal(LoginOutcomeKind.Authenticated, result.Kind);
        Assert.True(fixture.Session.IsAuthenticated);
        Assert.Equal("owner@example.test", fixture.Api.LastLoginRequest!.Email);
        Assert.Equal("Merkez", fixture.Session.CurrentBusiness!.Name);
        Assert.NotNull(fixture.Bootstrap.Subscription);
    }

    [Fact]
    public async Task InvalidCredentialsMapsToExpectedUserMessage()
    {
        var fixture = CreateFixture();
        fixture.Api.Login = (_, _) => Task.FromException<MobileAuthResponse>(
            new MobileApiException(
                "invalid_credentials",
                MobileErrorMessages.ForCode("invalid_credentials"),
                HttpStatusCode.Unauthorized));

        var result = await fixture.Service.LoginAsync("owner@example.test", "wrong");

        Assert.Equal(LoginOutcomeKind.Failed, result.Kind);
        Assert.Equal("E-posta veya şifre hatalı.", result.Message);
        Assert.False(fixture.Session.IsAuthenticated);
    }

    [Fact]
    public async Task BusinessSelectionRequiredCreatesExpectedState()
    {
        var fixture = CreateFixture();
        var businesses = new[]
        {
            new MobileBusinessOption(11, "Merkez", "Owner"),
            new MobileBusinessOption(12, "Şube", "Staff")
        };
        fixture.Api.Login = (_, _) => Task.FromException<MobileAuthResponse>(
            new MobileApiException(
                "business_selection_required",
                MobileErrorMessages.ForCode("business_selection_required"),
                HttpStatusCode.Conflict,
                businesses: businesses));

        var result = await fixture.Service.LoginAsync("owner@example.test", "secret");

        Assert.Equal(LoginOutcomeKind.BusinessSelectionRequired, result.Kind);
        Assert.Equal(2, fixture.Service.BusinessOptions.Count);
        Assert.False(fixture.Session.IsAuthenticated);
    }

    [Fact]
    public async Task BusinessSelectionCompletesLoginWithSelectedBusinessId()
    {
        var fixture = CreateFixture();
        fixture.Api.Login = (request, _) =>
        {
            if (request.BusinessId is null)
            {
                return Task.FromException<MobileAuthResponse>(
                    new MobileApiException(
                        "business_selection_required",
                        MobileErrorMessages.ForCode("business_selection_required"),
                        HttpStatusCode.Conflict,
                        businesses: [new MobileBusinessOption(12, "Şube", "Staff")]));
            }

            return Task.FromResult(TestData.Auth());
        };

        await fixture.Service.LoginAsync("owner@example.test", "secret");
        var result = await fixture.Service.SelectBusinessAsync(12);

        Assert.Equal(LoginOutcomeKind.Authenticated, result.Kind);
        Assert.Equal(12, fixture.Api.LastLoginRequest!.BusinessId);
        Assert.Empty(fixture.Service.BusinessOptions);
    }

    [Fact]
    public async Task RefreshTokenIsSavedToSecureTokenStore()
    {
        var fixture = CreateFixture();

        await fixture.Service.LoginAsync("owner@example.test", "secret");

        Assert.Equal("refresh-1", fixture.Store.Value!.RefreshToken);
        Assert.Equal(1, fixture.Store.SaveCount);
    }

    [Fact]
    public async Task AccessTokenExistsOnlyInMemorySession()
    {
        var fixture = CreateFixture();

        await fixture.Service.LoginAsync("owner@example.test", "secret");

        Assert.Equal("access-1", fixture.Session.AccessToken);
        Assert.DoesNotContain(
            "AccessToken",
            typeof(SecureTokenRecord).GetProperties().Select(property => property.Name));
    }

    [Fact]
    public async Task AppRestoreRefreshesSessionFromStoredRefreshToken()
    {
        var fixture = CreateFixture();
        fixture.Store.Value = new SecureTokenRecord("refresh-old", DateTime.UtcNow.AddDays(1));
        fixture.Api.Refresh = (token, _) =>
        {
            Assert.Equal("refresh-old", token);
            return Task.FromResult(TestData.Auth("restored"));
        };

        await fixture.Service.RestoreAsync();

        Assert.True(fixture.Session.IsRestoreComplete);
        Assert.True(fixture.Session.IsAuthenticated);
        Assert.Equal("access-restored", fixture.Session.AccessToken);
        Assert.Equal("refresh-restored", fixture.Store.Value!.RefreshToken);
    }

    [Fact]
    public async Task FailedRefreshClearsSecureStorage()
    {
        var fixture = CreateFixture();
        fixture.Store.Value = new SecureTokenRecord("refresh-old", DateTime.UtcNow.AddDays(1));
        fixture.Api.Refresh = (_, _) => Task.FromException<MobileAuthResponse>(
            new MobileApiException(
                "invalid_refresh_token",
                MobileErrorMessages.ForCode("invalid_refresh_token"),
                HttpStatusCode.Unauthorized));

        await fixture.Service.RestoreAsync();

        Assert.Null(fixture.Store.Value);
        Assert.True(fixture.Store.ClearCount > 0);
        Assert.False(fixture.Session.IsAuthenticated);
    }

    [Fact]
    public async Task LogoutClearsLocalSessionAndStorage()
    {
        var fixture = CreateFixture();
        await fixture.Service.LoginAsync("owner@example.test", "secret");

        await fixture.Service.LogoutAsync();

        Assert.Equal(1, fixture.Api.LogoutCalls);
        Assert.Null(fixture.Store.Value);
        Assert.False(fixture.Session.IsAuthenticated);
    }

    [Fact]
    public async Task LogoutAllClearsLocalSessionAndStorage()
    {
        var fixture = CreateFixture();
        await fixture.Service.LoginAsync("owner@example.test", "secret");

        await fixture.Service.LogoutAllAsync();

        Assert.Equal(1, fixture.Api.LogoutAllCalls);
        Assert.Null(fixture.Store.Value);
        Assert.False(fixture.Session.IsAuthenticated);
    }

    private static SessionFixture CreateFixture()
    {
        var api = new StubMobileApiClient();
        var store = new FakeSecureTokenStore();
        var session = new SessionState();
        var bootstrap = new BootstrapState();
        var tokenManager = new MobileTokenManager(session, store, api);
        var service = new MobileSessionService(api, tokenManager, store, session, bootstrap);
        return new SessionFixture(api, store, session, bootstrap, service);
    }

    private sealed record SessionFixture(
        StubMobileApiClient Api,
        FakeSecureTokenStore Store,
        SessionState Session,
        BootstrapState Bootstrap,
        MobileSessionService Service);
}
