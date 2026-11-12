using System.ComponentModel.DataAnnotations;
using BillFlow.Web.ApiClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BillFlow.Web.Pages;

public class LoginModel : PageModel
{
    private readonly BillFlowApiClient _api;

    public LoginModel(BillFlowApiClient api)
    {
        _api = api;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var auth = await _api.LoginAsync(new LoginRequest(Input.Email, Input.Password));
            await AuthSignIn.SignInAsync(HttpContext, auth);
            return RedirectToPage(auth.Role == "Admin" ? "/Admin/Plans" : "/Dashboard");
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
