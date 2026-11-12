using BillFlow.Web.ApiClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BillFlow.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CustomersModel : PageModel
{
    private const int PageSize = 20;

    private readonly BillFlowApiClient _api;

    public CustomersModel(BillFlowApiClient api)
    {
        _api = api;
    }

    public List<CustomerDto> Customers { get; private set; } = new();
    public int CurrentPage { get; private set; } = 1;
    public int TotalCount { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    public async Task OnGetAsync(int page = 1)
    {
        CurrentPage = Math.Max(1, page);
        var result = await _api.GetCustomersAsync(CurrentPage, PageSize);
        Customers = result.Items.ToList();
        TotalCount = result.TotalCount;
    }
}
