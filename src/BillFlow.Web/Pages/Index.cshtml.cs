using BillFlow.Web.ApiClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BillFlow.Web.Pages;

public class IndexModel : PageModel
{
    private readonly BillFlowApiClient _api;

    public IndexModel(BillFlowApiClient api)
    {
        _api = api;
    }

    public List<PlanDto> Plans { get; private set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Plans = await _api.GetPlansAsync();
    }

    public async Task<IActionResult> OnPostSubscribeAsync(int planId)
    {
        try
        {
            await _api.CreateSubscriptionAsync(new CreateSubscriptionRequest(planId));
            return RedirectToPage("/Dashboard");
        }
        catch (ApiException ex)
        {
            Plans = await _api.GetPlansAsync();
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}
