using System.Net;
using DukkanPilot.IntegrationTests.Infrastructure;

namespace DukkanPilot.IntegrationTests.MobileApi;

public sealed class MobileRegressionTests
{
    [Fact]
    public async Task Web_cookie_login_and_public_qr_menu_behaviors_remain_available()
    {
        using var factory = new DukkanPilotWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var loginPage = await client.GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.OK, loginPage.StatusCode);
        Assert.Equal("text/html", loginPage.Content.Headers.ContentType?.MediaType);

        var antiforgery = await AntiforgeryHelper.GetAsync(client, "/Account/Login");
        var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/Account/Login")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "owner.a@dukkanpilot.test",
                ["Password"] = MobileTestDataSeeder.Password,
                ["RememberMe"] = "false"
            })
        };
        AntiforgeryHelper.Add(loginRequest, antiforgery);
        using var loginResponse = await client.SendAsync(loginRequest);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        Assert.Equal("/Business/Dashboard", loginResponse.Headers.Location?.OriginalString);
        Assert.Contains(loginResponse.Headers, header =>
            header.Key == "Set-Cookie" && header.Value.Any(value => value.Contains("DukkanPilot.Auth=")));

        using var publicMenu = await client.GetAsync($"/m/{factory.Data.TenantASlug}");
        Assert.Equal(HttpStatusCode.OK, publicMenu.StatusCode);
        Assert.Equal("text/html", publicMenu.Content.Headers.ContentType?.MediaType);
    }
}
