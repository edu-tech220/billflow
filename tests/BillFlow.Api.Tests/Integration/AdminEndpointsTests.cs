using System.Net;
using System.Net.Http.Json;
using BillFlow.Api.Dtos;
using BillFlow.Api.Services;

namespace BillFlow.Api.Tests.Integration;

public class AdminEndpointsTests : IClassFixture<BillFlowWebApplicationFactory>
{
    private readonly BillFlowWebApplicationFactory _factory;

    public AdminEndpointsTests(BillFlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCustomers_AsAdmin_ReturnsPagedResult()
    {
        var client = _factory.CreateClient();
        var admin = await AuthTestHelper.LoginAsAdminAsync(client);
        AuthTestHelper.UseToken(client, admin.Token);
        await AuthTestHelper.RegisterAsync(_factory.CreateClient(), AuthTestHelper.UniqueEmail("admin-list"));

        var result = await client.GetFromJsonAsync<PagedResult<CustomerDto>>("api/customers?page=1&pageSize=5");

        Assert.NotNull(result);
        Assert.True(result!.TotalCount >= 2);
    }

    [Fact]
    public async Task GetCustomers_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var customer = await AuthTestHelper.RegisterAsync(client, AuthTestHelper.UniqueEmail("not-admin"));
        AuthTestHelper.UseToken(client, customer.Token);

        var response = await client.GetAsync("api/customers");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RunDunningSweep_AsAdmin_ReturnsAResult()
    {
        var client = _factory.CreateClient();
        var admin = await AuthTestHelper.LoginAsAdminAsync(client);
        AuthTestHelper.UseToken(client, admin.Token);

        var response = await client.PostAsJsonAsync("api/admin/dunning/run", new { });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<DunningSweepResult>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task RunDunningSweep_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var customer = await AuthTestHelper.RegisterAsync(client, AuthTestHelper.UniqueEmail("sweep-customer"));
        AuthTestHelper.UseToken(client, customer.Token);

        var response = await client.PostAsJsonAsync("api/admin/dunning/run", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
