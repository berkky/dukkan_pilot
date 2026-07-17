using System.Net;
using DukkanPilot.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace DukkanPilot.IntegrationTests.TenantSecurity;
public sealed class CrossTenantTableTests
{
 [Fact] public async Task Owner_cannot_read_or_mutate_foreign_table()
 {
  using var f=new DukkanPilotWebApplicationFactory();await f.InitializeAsync();using var c=TestClaims.CreateClient(f,TestUser.TenantAOwner);var d=f.Data;
  var get=await c.GetAsync($"/Business/Tables/Edit/{d.TenantBTable1Id}");Assert.Equal(HttpStatusCode.NotFound,get.StatusCode);Assert.DoesNotContain(d.TenantBTable1Label,await get.Content.ReadAsStringAsync());
  var token=await AntiforgeryHelper.GetAsync(c,"/Business/Tables");var edit=new HttpRequestMessage(HttpMethod.Post,$"/Business/Tables/Edit/{d.TenantBTable1Id}"){Content=new FormUrlEncodedContent(new Dictionary<string,string>{{"Id",d.TenantBTable1Id.ToString()},{"TableLabel","Leaked"},{"DisplayOrder","99"},{"IsActive","true"}})};AntiforgeryHelper.Add(edit,token);Assert.Equal(HttpStatusCode.NotFound,(await c.SendAsync(edit)).StatusCode);
  var toggle=new HttpRequestMessage(HttpMethod.Post,$"/Business/Tables/Toggle/{d.TenantBTable1Id}");AntiforgeryHelper.Add(toggle,token);Assert.Equal(HttpStatusCode.NotFound,(await c.SendAsync(toggle)).StatusCode);var qr=await c.GetAsync($"/Business/Tables/Qr/{d.TenantBTable1Id}");Assert.Equal(HttpStatusCode.NotFound,qr.StatusCode);Assert.DoesNotContain(d.TenantBTable1Code,await qr.Content.ReadAsStringAsync());
  var t=await f.DbAsync(x=>x.BusinessTables.AsNoTracking().SingleAsync(x=>x.Id==d.TenantBTable1Id));Assert.Equal(d.TenantBTable1Label,t.TableLabel);Assert.True(t.IsActive);
 }
}
