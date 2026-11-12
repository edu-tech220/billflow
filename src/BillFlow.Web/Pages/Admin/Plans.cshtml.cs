using System.ComponentModel.DataAnnotations;
using BillFlow.Web.ApiClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BillFlow.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class PlansModel : PageModel
{
    private readonly BillFlowApiClient _api;

    public PlansModel(BillFlowApiClient api)
    {
        _api = api;
    }

    public List<PlanDto> Plans { get; private set; } = new();

    [BindProperty]
    public NewPlanInput NewPlan { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        Plans = await _api.GetPlansAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            Plans = await _api.GetPlansAsync();
            return Page();
        }

        try
        {
            await _api.CreatePlanAsync(new CreatePlanRequest(NewPlan.Code, NewPlan.Name, NewPlan.MonthlyPrice, NewPlan.TrialDays));
            StatusMessage = $"Plan '{NewPlan.Code}' created.";
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }

        Plans = await _api.GetPlansAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id, string name, decimal monthlyPrice, bool isActive)
    {
        try
        {
            await _api.UpdatePlanAsync(id, new UpdatePlanRequest(name, monthlyPrice, isActive));
            StatusMessage = isActive ? "Plan activated." : "Plan deactivated.";
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }

        Plans = await _api.GetPlansAsync();
        return Page();
    }

    public class NewPlanInput
    {
        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, 100000)]
        public decimal MonthlyPrice { get; set; }

        [Range(0, 365)]
        public int TrialDays { get; set; }
    }
}
