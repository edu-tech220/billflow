using System.Net.Http.Headers;
using System.Net.Http.Json;
using BillFlow.Api.Data;
using BillFlow.Api.Dtos;

namespace BillFlow.Api.Tests.Integration;

public static class AuthTestHelper
{
    public static async Task<AuthResponse> RegisterAsync(HttpClient client, string email, string password = "Password1!", string name = "Test Customer")
    {
        var response = await client.PostAsJsonAsync("api/auth/register", new RegisterRequest(name, email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public static async Task<AuthResponse> LoginAsAdminAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("api/auth/login", new LoginRequest(DbSeeder.AdminEmail, DbSeeder.AdminPassword));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public static void UseToken(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@example.com";
}
