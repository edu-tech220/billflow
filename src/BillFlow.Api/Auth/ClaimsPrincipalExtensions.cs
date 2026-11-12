using System.Security.Claims;

namespace BillFlow.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static int GetCustomerId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The current principal has no NameIdentifier claim.");
        return int.Parse(value);
    }
}
