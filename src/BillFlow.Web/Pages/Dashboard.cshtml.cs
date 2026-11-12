using BillFlow.Web.ApiClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BillFlow.Web.Pages;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly BillFlowApiClient _api;

    public DashboardModel(BillFlowApiClient api)
    {
        _api = api;
    }

    public SubscriptionDto? Subscription { get; private set; }
    public List<PlanDto> Plans { get; private set; } = new();
    public List<InvoiceDto> Invoices { get; private set; } = new();
    public string? ErrorMessage { get; set; }
    public string? StatusMessage { get; set; }

    public string StatusBadgeClass => Subscription?.Status switch
    {
        "Active" => "badge-success",
        "Trialing" => "badge-neutral",
        "PastDue" => "badge-warning",
        "Canceled" or "Expired" => "badge-danger",
        _ => "badge-neutral",
    };

    public string InvoiceBadgeClass(string status) => status switch
    {
        "Paid" => "badge-success",
        "Open" => "badge-warning",
        "Void" or "Uncollectible" => "badge-danger",
        _ => "badge-neutral",
    };

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostChangePlanAsync(int newPlanId)
    {
        try
        {
            await _api.ChangePlanAsync(new ChangePlanRequest(newPlanId));
            StatusMessage = "Plan changed.";
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }

        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(bool atPeriodEnd)
    {
        try
        {
            await _api.CancelSubscriptionAsync(new CancelSubscriptionRequest(atPeriodEnd));
            StatusMessage = atPeriodEnd ? "Your subscription will cancel at the end of the period." : "Subscription canceled.";
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }

        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostPayAsync(int invoiceId, int attemptCount)
    {
        try
        {
            var result = await _api.PayInvoiceAsync(invoiceId, $"web-{invoiceId}-{attemptCount + 1}");
            StatusMessage = result.Succeeded ? "Payment succeeded." : $"Payment declined: {result.FailureReason}";
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }

        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        Subscription = await _api.GetMySubscriptionAsync();
        Plans = await _api.GetPlansAsync();
        var invoicePage = await _api.GetInvoicesAsync(page: 1, pageSize: 50);
        Invoices = invoicePage.Items.ToList();
    }
}
