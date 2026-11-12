using System.Net.Http.Headers;

namespace BillFlow.Web.ApiClient;

/// <summary>
/// Attaches the JWT BillFlow.Web obtained from the API at login time (stored as a claim in
/// the user's authentication cookie) to every outgoing request, so the Web project never
/// needs its own copy of the signing key.
/// </summary>
public class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BearerTokenHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _httpContextAccessor.HttpContext?.User.FindFirst("access_token")?.Value;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
