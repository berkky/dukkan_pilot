using System.Net;
using DukkanPilot.Core.Enums;
using DukkanPilot.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace DukkanPilot.IntegrationTests.TenantSecurity;
public sealed class CrossTenantOrderTests
{
 [Theory][InlineData(TestUser.TenantAOwner)][InlineData(TestUser.TenantAStaff)] public async Task TenantA_cannot_read_or_update_tenantB_order(TestUser u)
 {using var f=new DukkanPilotWebApplicationFactory();await f.InitializeAsync();using var c=TestClaims.CreateClient(f,u);var d=f.Data;var detail=await c.GetAsync($"/Business/Orders/Details/{d.TenantBOrderId}");Assert.Equal(HttpStatusCode.NotFound,detail.StatusCode);Assert.DoesNotContain(d.TenantBOrderNumber,await detail.Content.ReadAsStringAsync());var kitchen=await c.GetAsync("/Business/Orders/Kitchen");Assert.Equal(HttpStatusCode.OK,kitchen.StatusCode);Assert.DoesNotContain(d.TenantBCustomerName,await kitchen.Content.ReadAsStringAsync());var t=await AntiforgeryHelper.GetAsync(c,"/Business/Orders");var req=new HttpRequestMessage(HttpMethod.Post,$"/Business/Orders/UpdateStatus/{d.TenantBOrderId}"){Content=new FormUrlEncodedContent(new Dictionary<string,string>{{"status",nameof(OrderStatus.Completed)}})};AntiforgeryHelper.Add(req,t);Assert.Equal(HttpStatusCode.NotFound,(await c.SendAsync(req)).StatusCode);Assert.Equal(OrderStatus.Pending,await f.DbAsync(x=>x.Orders.Where(o=>o.Id==d.TenantBOrderId).Select(o=>o.Status).SingleAsync()));}
}
