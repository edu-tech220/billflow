using System.Net;
using System.Net.Http.Json;
using BillFlow.Api.Dtos;

namespace BillFlow.Api.Tests.Integration;

public class PlansEndpointsTests : IClassFixture<BillFlowWebApplicationFactory>
{
    private readonly BillFlowWebApplicationFactory _factory;

    public PlansEndpointsTests(BillFlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPlans_Anonymous_ReturnsSeededActivePlans()
    {
        var client = _factory.CreateClient();

        var plans = await client.GetFromJsonAsync<List<PlanDto>>("api/plans");

        Assert.NotNull(plans);
        Assert.Contains(plans!, p => p.Code == "starter");
        Assert.All(plans!, p => Assert.True(p.IsActive));
    }

    [Fact]
    public async Task CreatePlan_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var auth = await AuthTestHelper.RegisterAsync(client, AuthTestHelper.UniqueEmail("plan-customer"));
        AuthTestHelper.UseToken(client, auth.Token);

        var response = await client.PostAsJsonAsync("api/plans", new CreatePlanRequest("new-plan", "New Plan", 15m, 0));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreatePlan_AsAdmin_AppearsInCatalog()
    {
        var client = _factory.CreateClient();
        var admin = await AuthTestHelper.LoginAsAdminAsync(client);
        AuthTestHelper.UseToken(client, admin.Token);
        var code = $"plan-{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("api/plans", new CreatePlanRequest(code, "Integration Plan", 42m, 0));
        createResponse.EnsureSuccessStatusCode();

        var plans = await client.GetFromJsonAsync<List<PlanDto>>("api/plans");
        Assert.Contains(plans!, p => p.Code == code && p.MonthlyPrice == 42m);
    }

    [Fact]
    public async Task DeactivatedPlan_DisappearsFromAnonymousCatalog_ButStaysVisibleToAdmin()
    {
        var client = _factory.CreateClient();
        var admin = await AuthTestHelper.LoginAsAdminAsync(client);
        AuthTestHelper.UseToken(client, admin.Token);
        var code = $"plan-{Guid.NewGuid():N}";

        var created = await (await client.PostAsJsonAsync("api/plans", new CreatePlanRequest(code, "Temp Plan", 20m, 0)))
            .Content.ReadFromJsonAsync<PlanDto>();
        await client.PutAsJsonAsync($"api/plans/{created!.Id}", new UpdatePlanRequest(created.Name, created.MonthlyPrice, IsActive: false));

        var adminView = await client.GetFromJsonAsync<List<PlanDto>>("api/plans");
        Assert.Contains(adminView!, p => p.Code == code);

        var anonymousClient = _factory.CreateClient();
        var anonymousView = await anonymousClient.GetFromJsonAsync<List<PlanDto>>("api/plans");
        Assert.DoesNotContain(anonymousView!, p => p.Code == code);
    }
}
