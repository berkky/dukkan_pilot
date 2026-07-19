using System.Net;
using DukkanPilot.Mobile.Core.Api;
using DukkanPilot.Mobile.Core.Contracts;
using DukkanPilot.Mobile.Core.State;

namespace DukkanPilot.Mobile.Tests;

public sealed class OrderStateTests
{
    [Fact]
    public async Task PaginationResultsAreMergedInOrder()
    {
        var api = new StubMobileApiClient
        {
            Orders = (page, pageSize, _, _) => Task.FromResult(
                new PagedResponse<MobileOrderListItem>(
                    page,
                    pageSize,
                    4,
                    page == 1
                        ? [TestData.ListOrder(1), TestData.ListOrder(2)]
                        : [TestData.ListOrder(3), TestData.ListOrder(4)]))
        };
        var state = new OrderState(api, new FakeConnectivityService());

        await state.LoadFirstPageAsync();
        await state.LoadMoreAsync();

        Assert.Equal([1, 2, 3, 4], state.Items.Select(order => order.Id));
        Assert.False(state.HasMore);
    }

    [Fact]
    public async Task DuplicateOrdersAreNotAddedAcrossPages()
    {
        var api = new StubMobileApiClient
        {
            Orders = (page, pageSize, _, _) => Task.FromResult(
                new PagedResponse<MobileOrderListItem>(
                    page,
                    pageSize,
                    3,
                    page == 1
                        ? [TestData.ListOrder(1), TestData.ListOrder(2)]
                        : [TestData.ListOrder(2), TestData.ListOrder(3)]))
        };
        var state = new OrderState(api, new FakeConnectivityService());

        await state.LoadFirstPageAsync();
        await state.LoadMoreAsync();

        Assert.Equal(3, state.Items.Count);
        Assert.Equal(3, state.Items.Select(order => order.Id).Distinct().Count());
    }

    [Fact]
    public async Task TenantBusinessIdIsNotAddedToOrderQueryOrBody()
    {
        var handler = new RecordingHttpMessageHandler((_, _, _) =>
            Task.FromResult(RecordingHttpMessageHandler.Json(
                new PagedResponse<MobileOrderListItem>(1, 20, 0, []))));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.test/")
        };
        var api = new MobileApiClient(httpClient);

        await api.GetOrdersAsync(1, 20, "Pending");

        var request = handler.Requests.Single();
        Assert.DoesNotContain("business", request.PathAndQuery, StringComparison.OrdinalIgnoreCase);
        Assert.Null(request.Body);
    }

    [Fact]
    public async Task OrderDetailMapsAllContractFields()
    {
        var expected = TestData.Details(42);
        var handler = new RecordingHttpMessageHandler((_, _, _) =>
            Task.FromResult(RecordingHttpMessageHandler.Json(expected)));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.test/")
        };
        var api = new MobileApiClient(httpClient);

        var actual = await api.GetOrderAsync(42);

        Assert.Equal(expected.OrderNumber, actual.OrderNumber);
        Assert.Equal(expected.TableLabel, actual.TableLabel);
        Assert.Equal(expected.Items.Single().LineTotal, actual.Items.Single().LineTotal);
        Assert.Equal("/api/mobile/v1/orders/42", handler.Requests.Single().PathAndQuery);
    }

    [Fact]
    public async Task StatusUpdateSuccessUpdatesState()
    {
        var api = new StubMobileApiClient
        {
            Orders = (_, pageSize, _, _) => Task.FromResult(
                new PagedResponse<MobileOrderListItem>(
                    1,
                    pageSize,
                    1,
                    [TestData.ListOrder(1)])),
            UpdateStatus = (id, status, _) => Task.FromResult(TestData.Details(id, status))
        };
        var state = new OrderState(api, new FakeConnectivityService());
        await state.LoadFirstPageAsync();
        await state.LoadDetailsAsync(1);

        var succeeded = await state.UpdateStatusAsync(1, "Preparing");

        Assert.True(succeeded);
        Assert.Equal("Preparing", state.SelectedOrder!.Status);
        Assert.Equal("Preparing", state.Items.Single().Status);
    }

    [Fact]
    public async Task StatusUpdateFailureKeepsPreviousState()
    {
        var api = new StubMobileApiClient
        {
            UpdateStatus = (_, _, _) => Task.FromException<MobileOrderDetails>(
                new MobileApiException(
                    "invalid_order_status",
                    MobileErrorMessages.ForCode("invalid_order_status"),
                    HttpStatusCode.BadRequest))
        };
        var state = new OrderState(api, new FakeConnectivityService());
        await state.LoadDetailsAsync(1);

        var succeeded = await state.UpdateStatusAsync(1, "Completed");

        Assert.False(succeeded);
        Assert.Equal("Pending", state.SelectedOrder!.Status);
        Assert.Equal("Bu sipariş durum değişikliği yapılamıyor.", state.LastError);
    }

    [Fact]
    public async Task ForeignTenantNotFoundProducesSafeBehavior()
    {
        var api = new StubMobileApiClient
        {
            Order = (_, _) => Task.FromException<MobileOrderDetails>(
                new MobileApiException(
                    "resource_not_found",
                    MobileErrorMessages.ForCode("resource_not_found"),
                    HttpStatusCode.NotFound,
                    "trace-safe"))
        };
        var state = new OrderState(api, new FakeConnectivityService());

        var exception = await Assert.ThrowsAsync<MobileApiException>(
            () => state.LoadDetailsAsync(999));

        Assert.Equal("resource_not_found", exception.Code);
        Assert.Null(state.SelectedOrder);
        Assert.Equal("İstenen kayıt bulunamadı veya artık erişilemiyor.", state.LastError);
        Assert.DoesNotContain("tenant", state.LastError!, StringComparison.OrdinalIgnoreCase);
    }
}
