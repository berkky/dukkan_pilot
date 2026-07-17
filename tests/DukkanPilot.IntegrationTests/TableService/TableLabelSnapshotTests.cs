using DukkanPilot.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace DukkanPilot.IntegrationTests.TableService;
public sealed class TableLabelSnapshotTests
{
 [Fact] public async Task Rename_does_not_change_existing_order_snapshot(){using var f=new DukkanPilotWebApplicationFactory();await f.InitializeAsync();var o=await PublicTableOrderTests.Place(f,f.Data.TenantATable1Code,"ignore");Assert.Equal("A Masa 1",o.TableLabelSnapshot);await f.DbAsync(async db=>{var t=await db.BusinessTables.SingleAsync(x=>x.Id==f.Data.TenantATable1Id);t.TableLabel="A Salon Masasi";await db.SaveChangesAsync();});var s=await f.DbAsync(async db=>new{Table=await db.BusinessTables.Where(x=>x.Id==f.Data.TenantATable1Id).Select(x=>x.TableLabel).SingleAsync(),Snapshot=await db.Orders.Where(x=>x.Id==o.Id).Select(x=>x.TableLabelSnapshot).SingleAsync()});Assert.Equal("A Salon Masasi",s.Table);Assert.Equal("A Masa 1",s.Snapshot);using var c=TestClaims.CreateClient(f,TestUser.TenantAOwner);var body=await (await c.GetAsync($"/Business/Orders/Details/{o.Id}")).Content.ReadAsStringAsync();Assert.Contains("A Masa 1",body);Assert.DoesNotContain("A Salon Masasi",body);}
}
