using System.Net.Http.Json;
using System.Text.Json;

namespace BillFlow.Web.ApiClient;

/// <summary>Thin typed wrapper around the BillFlow.Api HTTP surface for the Razor Pages frontend.</summary>
public class BillFlowApiClient
{
    private readonly HttpClient _http;

    public BillFlowApiClient(HttpClient http)
    {
        _http = http;
    }

    public Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default) =>
        PostAsync<AuthResponse>("api/auth/register", request, ct);

    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default) =>
        PostAsync<AuthResponse>("api/auth/login", request, ct);

    public Task<List<PlanDto>> GetPlansAsync(CancellationToken ct = default) =>
        GetAsync<List<PlanDto>>("api/plans", ct);

    public Task<PlanDto> CreatePlanAsync(CreatePlanRequest request, CancellationToken ct = default) =>
        PostAsync<PlanDto>("api/plans", request, ct);

    public Task<PlanDto> UpdatePlanAsync(int id, UpdatePlanRequest request, CancellationToken ct = default) =>
        PutAsync<PlanDto>($"api/plans/{id}", request, ct);

    public Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionRequest request, CancellationToken ct = default) =>
        PostAsync<SubscriptionDto>("api/subscriptions", request, ct);

    public async Task<SubscriptionDto?> GetMySubscriptionAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/subscriptions/me", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<SubscriptionDto>(cancellationToken: ct);
    }

    public Task<SubscriptionDto> ChangePlanAsync(ChangePlanRequest request, CancellationToken ct = default) =>
        PostAsync<SubscriptionDto>("api/subscriptions/me/change-plan", request, ct);

    public Task<SubscriptionDto> CancelSubscriptionAsync(CancelSubscriptionRequest request, CancellationToken ct = default) =>
        PostAsync<SubscriptionDto>("api/subscriptions/me/cancel", request, ct);

    public Task<PagedResult<InvoiceDto>> GetInvoicesAsync(int page = 1, int pageSize = 20, int? customerId = null, CancellationToken ct = default)
    {
        var query = customerId is null
            ? $"api/invoices?page={page}&pageSize={pageSize}"
            : $"api/invoices?page={page}&pageSize={pageSize}&customerId={customerId}";
        return GetAsync<PagedResult<InvoiceDto>>(query, ct);
    }

    public async Task<PaymentResultDto> PayInvoiceAsync(int invoiceId, string idempotencyKey, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/invoices/{invoiceId}/payments");
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<PaymentResultDto>(cancellationToken: ct))!;
    }

    public Task<PagedResult<CustomerDto>> GetCustomersAsync(int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<CustomerDto>>($"api/customers?page={page}&pageSize={pageSize}", ct);

    public Task<DunningSweepResult> RunDunningSweepAsync(CancellationToken ct = default) =>
        PostAsync<DunningSweepResult>("api/admin/dunning/run", new { }, ct);

    private async Task<T> GetAsync<T>(string url, CancellationToken ct)
    {
        var response = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct))!;
    }

    private async Task<T> PostAsync<T>(string url, object body, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync(url, body, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct))!;
    }

    private async Task<T> PutAsync<T>(string url, object body, CancellationToken ct)
    {
        var response = await _http.PutAsJsonAsync(url, body, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct))!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var detail = response.ReasonPhrase ?? "The request failed.";
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblem>(cancellationToken: ct);
            if (!string.IsNullOrWhiteSpace(problem?.Detail))
            {
                detail = problem.Detail;
            }
        }
        catch (JsonException)
        {
            // Response body wasn't a ProblemDetails payload; fall back to the reason phrase.
        }

        throw new ApiException((int)response.StatusCode, detail);
    }
}
