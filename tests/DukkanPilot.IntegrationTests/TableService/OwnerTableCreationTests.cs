using System.Net;
using DukkanPilot.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.IntegrationTests.TableService;

public sealed class OwnerTableCreationTests
{
    [Fact]
    public async Task Owner_can_create_a_table_for_own_tenant_only()
    {
        using var factory = new DukkanPilotWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = TestClaims.CreateClient(factory, TestUser.TenantAOwner);
        var token = await AntiforgeryHelper.GetAsync(client, "/Business/Tables");
        var request = new HttpRequestMessage(HttpMethod.Post, "/Business/Tables/Create") { Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["TableLabel"] = "Created by owner", ["DisplayOrder"] = "7", ["IsActive"] = "true", ["PublicCode"] = "FORGED-CODE" }) };
        AntiforgeryHelper.Add(request, token);
        Assert.Equal(HttpStatusCode.Found, (await client.SendAsync(request)).StatusCode);
        var table = await factory.DbAsync(db => db.BusinessTables.SingleAsync(x => x.BusinessId == factory.Data.TenantAId && x.TableLabel == "Created by owner"));
        Assert.NotEqual("FORGED-CODE", table.PublicCode);
        Assert.True(table.IsActive);
    }
}

