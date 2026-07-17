using System.Net;
using System.Text;
using System.Text.Json;
using DukkanPilot.Core.Common;
using DukkanPilot.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace DukkanPilot.IntegrationTests.TableService;
public sealed class PublicTableOrderTests
{
 [Theory][InlineData("NOT-A-TABLE")][InlineData("B-TABLE-1")] public async Task Invalid_or_foreign_code_is_not_bound(string code){using var f=new DukkanPilotWebApplicationFactory();await f.InitializeAsync();var o=await Place(f,code,"Forged");Assert.Null(o.BusinessTableId);Assert.Null(o.TableLabelSnapshot);Assert.Null(o.ServiceType);}
 [Fact] public async Task Valid_code_uses_server_table_snapshot(){using var f=new DukkanPilotWebApplicationFactory();await f.InitializeAsync();var o=await Place(f,f.Data.TenantATable1Code,"Forged");Assert.Equal(f.Data.TenantATable1Id,o.BusinessTableId);Assert.Equal("A Masa 1",o.TableLabelSnapshot);Assert.Equal(OrderServiceTypes.TableService,o.ServiceType);}
 internal static async Task<DukkanPilot.Core.Entities.Order> Place(DukkanPilotWebApplicationFactory f,string code,string forged){using var c=f.CreateClient();var d=f.Data;var t=await AntiforgeryHelper.GetAsync(c,$"/m/{d.TenantASlug}");var json=JsonSerializer.Serialize(new{items=new[]{new{productId=d.TenantAProductId,quantity=1}},tablePublicCode=code,tableLabel=forged});var r=new HttpRequestMessage(HttpMethod.Post,$"/m/{d.TenantASlug}/order"){Content=new StringContent(json,Encoding.UTF8,"application/json")};AntiforgeryHelper.Add(r,t);Assert.Equal(HttpStatusCode.OK,(await c.SendAsync(r)).StatusCode);return await f.DbAsync(x=>x.Orders.AsNoTracking().OrderByDescending(o=>o.Id).FirstAsync(o=>o.BusinessId==d.TenantAId));}
}
