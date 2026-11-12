namespace BillFlow.Web.ApiClient;

/// <summary>Carries the backend's ProblemDetails (or a generic message) up to the page that can render it.</summary>
public class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
