using System.ComponentModel.DataAnnotations;
using BillFlow.Web.ApiClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BillFlow.Web.Pages;

public class RegisterModel : PageModel
{
    private readonly BillFlowApiClient _api;

    public RegisterModel(BillFlowApiClient api)
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
            var auth = await _api.RegisterAsync(new RegisterRequest(Input.Name, Input.Email, Input.Password));
            await AuthSignIn.SignInAsync(HttpContext, auth);
            return RedirectToPage("/Dashboard");
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    public class InputModel
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string Password { get; set; } = string.Empty;
    }
}
