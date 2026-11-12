using System.Net;
using System.Net.Http.Json;
using BillFlow.Api.Dtos;

namespace BillFlow.Api.Tests.Integration;

public class SubscriptionsAndInvoicesEndpointsTests : IClassFixture<BillFlowWebApplicationFactory>
{
    private readonly BillFlowWebApplicationFactory _factory;

    public SubscriptionsAndInvoicesEndpointsTests(BillFlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullSubscriptionLifecycle_SubscribePayUpgradeCancel_WorksEndToEnd()
    {
        var adminClient = _factory.CreateClient();
        var admin = await AuthTestHelper.LoginAsAdminAsync(adminClient);
        AuthTestHelper.UseToken(adminClient, admin.Token);

        var premiumCode = $"premium-{Guid.NewGuid():N}";
        var createPremiumResponse = await adminClient.PostAsJsonAsync("api/plans", new CreatePlanRequest(premiumCode, "Premium", 199m, 0));
        var premiumPlan = await createPremiumResponse.Content.ReadFromJsonAsync<PlanDto>();

        var plans = await adminClient.GetFromJsonAsync<List<PlanDto>>("api/plans");
        var enterprisePlan = plans!.Single(p => p.Code == "enterprise");

        var client = _factory.CreateClient();
        var customer = await AuthTestHelper.RegisterAsync(client, AuthTestHelper.UniqueEmail("lifecycle"));
        AuthTestHelper.UseToken(client, customer.Token);

        var subscribeResponse = await client.PostAsJsonAsync("api/subscriptions", new CreateSubscriptionRequest(enterprisePlan.Id));
        subscribeResponse.EnsureSuccessStatusCode();
        var subscription = await subscribeResponse.Content.ReadFromJsonAsync<SubscriptionDto>();
        Assert.Equal("Active", subscription!.Status);

        var invoicesPage = await client.GetFromJsonAsync<PagedResult<InvoiceDto>>("api/invoices?page=1&pageSize=10");
        var invoice = Assert.Single(invoicesPage!.Items);
        Assert.Equal("Paid", invoice.Status);

        // Replaying the same idempotency key against an already-paid invoice must stay a no-op success.
        using var payRequest = new HttpRequestMessage(HttpMethod.Post, $"api/invoices/{invoice.Id}/payments");
        payRequest.Headers.Add("Idempotency-Key", "replay-key-1");
        var payResponse = await client.SendAsync(payRequest);
        payResponse.EnsureSuccessStatusCode();
        var paymentResult = await payResponse.Content.ReadFromJsonAsync<PaymentResultDto>();
        Assert.True(paymentResult!.Succeeded);

        var changePlanResponse = await client.PostAsJsonAsync("api/subscriptions/me/change-plan", new ChangePlanRequest(premiumPlan!.Id));
        changePlanResponse.EnsureSuccessStatusCode();
        var changed = await changePlanResponse.Content.ReadFromJsonAsync<SubscriptionDto>();
        Assert.Equal(premiumCode, changed!.PlanCode);

        var invoicesAfterUpgrade = await client.GetFromJsonAsync<PagedResult<InvoiceDto>>("api/invoices?page=1&pageSize=10");
        Assert.Contains(invoicesAfterUpgrade!.Items, i => i.Type == "Proration" && i.Status == "Paid");

        var cancelResponse = await client.PostAsJsonAsync("api/subscriptions/me/cancel", new CancelSubscriptionRequest(AtPeriodEnd: false));
        cancelResponse.EnsureSuccessStatusCode();
        var canceled = await cancelResponse.Content.ReadFromJsonAsync<SubscriptionDto>();
        Assert.Equal("Canceled", canceled!.Status);
    }

    [Fact]
    public async Task GetMySubscription_WithNoSubscription_ReturnsNotFoundProblemDetails()
    {
        var client = _factory.CreateClient();
        var customer = await AuthTestHelper.RegisterAsync(client, AuthTestHelper.UniqueEmail("none"));
        AuthTestHelper.UseToken(client, customer.Token);

        var response = await client.GetAsync("api/subscriptions/me");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CreateSubscription_WhenAlreadySubscribed_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var customer = await AuthTestHelper.RegisterAsync(client, AuthTestHelper.UniqueEmail("double-sub"));
        AuthTestHelper.UseToken(client, customer.Token);
        var plans = await client.GetFromJsonAsync<List<PlanDto>>("api/plans");
        var starter = plans!.Single(p => p.Code == "starter");

        await client.PostAsJsonAsync("api/subscriptions", new CreateSubscriptionRequest(starter.Id));
        var secondResponse = await client.PostAsJsonAsync("api/subscriptions", new CreateSubscriptionRequest(starter.Id));

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task GetInvoiceById_AsAnotherCustomer_ReturnsForbidden()
    {
        var clientA = _factory.CreateClient();
        var customerA = await AuthTestHelper.RegisterAsync(clientA, AuthTestHelper.UniqueEmail("iso-a"));
        AuthTestHelper.UseToken(clientA, customerA.Token);

        var plans = await clientA.GetFromJsonAsync<List<PlanDto>>("api/plans");
        var enterprisePlan = plans!.Single(p => p.Code == "enterprise");
        await clientA.PostAsJsonAsync("api/subscriptions", new CreateSubscriptionRequest(enterprisePlan.Id));
        var invoicesA = await clientA.GetFromJsonAsync<PagedResult<InvoiceDto>>("api/invoices?page=1&pageSize=10");
        var invoiceId = invoicesA!.Items.Single().Id;

        var clientB = _factory.CreateClient();
        var customerB = await AuthTestHelper.RegisterAsync(clientB, AuthTestHelper.UniqueEmail("iso-b"));
        AuthTestHelper.UseToken(clientB, customerB.Token);

        var response = await clientB.GetAsync($"api/invoices/{invoiceId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
