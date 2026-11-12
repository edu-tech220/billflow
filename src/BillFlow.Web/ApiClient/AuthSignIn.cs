using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BillFlow.Web.ApiClient;

/// <summary>
/// Turns the JWT the API issued into the Web project's own auth cookie: the token's claims
/// (name, email, role) become the cookie's claims for display/authorization, and the raw
/// token is tucked away as an extra claim so BearerTokenHandler can forward it on API calls.
/// </summary>
public static class AuthSignIn
{
    public static async Task SignInAsync(HttpContext httpContext, AuthResponse auth)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(auth.Token);
        var claims = new List<Claim>(jwt.Claims) { new("access_token", auth.Token) };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            ExpiresUtc = auth.ExpiresAt,
            IsPersistent = false,
        });
    }
}
