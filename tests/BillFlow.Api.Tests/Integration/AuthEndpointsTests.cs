using System.Net;
using System.Net.Http.Json;
using BillFlow.Api.Dtos;

namespace BillFlow.Api.Tests.Integration;

public class AuthEndpointsTests : IClassFixture<BillFlowWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(BillFlowWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ThenLogin_BothReturnAUsableToken()
    {
        var email = AuthTestHelper.UniqueEmail("register");

        var registered = await AuthTestHelper.RegisterAsync(_client, email);
        Assert.False(string.IsNullOrWhiteSpace(registered.Token));
        Assert.Equal("Customer", registered.Role);

        var loginResponse = await _client.PostAsJsonAsync("api/auth/login", new LoginRequest(email, "Password1!"));
        loginResponse.EnsureSuccessStatusCode();
        var loggedIn = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.False(string.IsNullOrWhiteSpace(loggedIn!.Token));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var email = AuthTestHelper.UniqueEmail("dup");
        await AuthTestHelper.RegisterAsync(_client, email);

        var response = await _client.PostAsJsonAsync("api/auth/register", new RegisterRequest("Someone Else", email, "Password1!"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = AuthTestHelper.UniqueEmail("badpw");
        await AuthTestHelper.RegisterAsync(_client, email);

        var response = await _client.PostAsJsonAsync("api/auth/login", new LoginRequest(email, "WrongPassword1!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("api/subscriptions/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
