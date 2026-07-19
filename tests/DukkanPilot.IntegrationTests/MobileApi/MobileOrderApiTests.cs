using System.Net;
using System.Net.Http.Json;
using DukkanPilot.Core.Enums;
using DukkanPilot.IntegrationTests.Infrastructure;
using DukkanPilot.Web.Api.Mobile.V1.Contracts.Common;
using DukkanPilot.Web.Api.Mobile.V1.Contracts.Orders;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.IntegrationTests.MobileApi;

public sealed class MobileOrderApiTests
{
    [Theory]
    [InlineData("owner.a@dukkanpilot.test")]
    [InlineData("staff.a@dukkanpilot.test")]
    public async Task Owner_and_staff_can_access_tenant_order_endpoints(string email)
    {
        using var factory = new DukkanPilotWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        var login = await MobileTestAuth.LoginSuccessAsync(client, email, factory.Data.TenantAId);
        MobileTestAuth.UseBearer(client, login.AccessToken);

        using var orders = await client.GetAsync("/api/mobile/v1/orders?page=1&pageSize=20");
        using var kitchen = await client.GetAsync("/api/mobile/v1/kitchen/orders");
        using var dashboard = await client.GetAsync("/api/mobile/v1/dashboard/today");
        using var bootstrap = await client.GetAsync("/api/mobile/v1/bootstrap");

        Assert.Equal(HttpStatusCode.OK, orders.StatusCode);
        Assert.Equal(HttpStatusCode.OK, kitchen.StatusCode);
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        Assert.Equal(HttpStatusCode.OK, bootstrap.StatusCode);
    }

    [Fact]
    public async Task Tenant_claim_blocks_foreign_list_detail_status_and_request_business_override()
    {
        using var factory = new DukkanPilotWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        var login = await MobileTestAuth.LoginSuccessAsync(
            client,
            "owner.a@dukkanpilot.test",
            factory.Data.TenantAId);
        MobileTestAuth.UseBearer(client, login.AccessToken);

        using var listResponse = await client.GetAsync(
            $"/api/mobile/v1/orders?BusinessId={factory.Data.TenantBId}&page=1&pageSize=100");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResponse<MobileOrderListItem>>();
        Assert.NotNull(page);
        Assert.DoesNotContain(page.Items, order => order.OrderNumber == factory.Data.TenantBOrderNumber);
        Assert.DoesNotContain(page.Items, order => order.CustomerName == factory.Data.TenantBCustomerName);

        using var detail = await client.GetAsync($"/api/mobile/v1/orders/{factory.Data.TenantBOrderId}");
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
        Assert.Equal("resource_not_found", await MobileAuthTests.ReadCodeAsync(detail));

        using var update = await client.PostAsJsonAsync(
            $"/api/mobile/v1/orders/{factory.Data.TenantBOrderId}/status",
            new { status = nameof(OrderStatus.Preparing), businessId = factory.Data.TenantBId });
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(OrderStatus.Pending, await factory.DbAsync(db => db.Orders
            .Where(order => order.Id == factory.Data.TenantBOrderId)
            .Select(order => order.Status)
            .SingleAsync()));

        using var kitchenResponse = await client.GetAsync("/api/mobile/v1/kitchen/orders");
        var kitchenBody = await kitchenResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(factory.Data.TenantBOrderNumber, kitchenBody);
        Assert.DoesNotContain(factory.Data.TenantBCustomerName, kitchenBody);

        using var dashboardResponse = await client.GetAsync("/api/mobile/v1/dashboard/today");
        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<MobileDashboardTodayResponse>();
        Assert.NotNull(dashboard);
        Assert.Equal(1, dashboard.TotalOrders);
        Assert.Equal(50m, dashboard.Revenue);
    }

    [Fact]
    public async Task Pagination_detail_snapshot_and_status_transition_behave_consistently()
    {
        using var factory = new DukkanPilotWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        var login = await MobileTestAuth.LoginSuccessAsync(
            client,
            "staff.a@dukkanpilot.test",
            factory.Data.TenantAId);
        MobileTestAuth.UseBearer(client, login.AccessToken);

        var tenantAOrderId = await factory.DbAsync(db => db.Orders
            .Where(order => order.BusinessId == factory.Data.TenantAId)
            .Select(order => order.Id)
            .SingleAsync());

        using var pageResponse = await client.GetAsync("/api/mobile/v1/orders?page=1&pageSize=1");
        var page = await pageResponse.Content.ReadFromJsonAsync<PagedResponse<MobileOrderListItem>>();
        Assert.NotNull(page);
        Assert.Equal(1, page.Page);
        Assert.Equal(1, page.PageSize);
        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);

        using var detailResponse = await client.GetAsync($"/api/mobile/v1/orders/{tenantAOrderId}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<MobileOrderDetails>();
        Assert.NotNull(detail);
        Assert.Equal("TableService", detail.ServiceType);
        Assert.Equal(factory.Data.TenantATable1Label, detail.TableLabel);
        Assert.Single(detail.Items);

        using var preparing = await client.PostAsJsonAsync(
            $"/api/mobile/v1/orders/{tenantAOrderId}/status",
            new { status = nameof(OrderStatus.Preparing), businessId = factory.Data.TenantBId });
        Assert.Equal(HttpStatusCode.OK, preparing.StatusCode);
        var updated = await preparing.Content.ReadFromJsonAsync<MobileOrderDetails>();
        Assert.Equal(nameof(OrderStatus.Preparing), updated?.Status);

        using var completed = await client.PostAsJsonAsync(
            $"/api/mobile/v1/orders/{tenantAOrderId}/status",
            new { status = nameof(OrderStatus.Completed) });
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);

        using var invalidTransition = await client.PostAsJsonAsync(
            $"/api/mobile/v1/orders/{tenantAOrderId}/status",
            new { status = nameof(OrderStatus.Pending) });
        Assert.Equal(HttpStatusCode.BadRequest, invalidTransition.StatusCode);
        Assert.Equal("invalid_order_status", await MobileAuthTests.ReadCodeAsync(invalidTransition));

        using var invalidStatus = await client.PostAsJsonAsync(
            $"/api/mobile/v1/orders/{tenantAOrderId}/status",
            new { status = "InventedStatus" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidStatus.StatusCode);
        Assert.Equal("invalid_order_status", await MobileAuthTests.ReadCodeAsync(invalidStatus));
    }
}
