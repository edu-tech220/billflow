using BillFlow.Web.ApiClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BillFlow.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DunningModel : PageModel
{
    private readonly BillFlowApiClient _api;

    public DunningModel(BillFlowApiClient api)
    {
        _api = api;
    }

    public DunningSweepResult? Result { get; private set; }
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            Result = await _api.RunDunningSweepAsync();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }

        return Page();
    }
}
