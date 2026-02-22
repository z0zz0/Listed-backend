using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Listed.API.Contracts.Auth;
using Listed.API.Contracts.Users;
using Listed.Testing.Factories;

namespace Listed.API.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class AuthEndpointTests : IClassFixture<ApiWebApplicationFactory>, IAsyncLifetime
{
    private readonly ApiWebApplicationFactory _factory;

    public AuthEndpointTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessToken_AndSetsRefreshCookie()
    {
        using var client = _factory.CreateClient();

        var email = LoginRequestFactory.CreateEmail("auth-login");
        var password = "StrongPass123!";
        await CreateUserAsync(client, email, password);

        var response = await client.PostAsJsonAsync("/api/auth/login", LoginRequestFactory.Valid(email, password));
        var body = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookieValues));
        Assert.Contains(setCookieValues!, header => header.Contains("listed_refresh_token=", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(setCookieValues!, header => header.Contains("listed_device_id=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_WhenAlreadyLoggedInOnSameDevice_ReturnsSuccessWithSameRefreshSession()
    {
        using var client = _factory.CreateClient();

        var email = LoginRequestFactory.CreateEmail("auth-login-dupe");
        var password = "StrongPass123!";
        await CreateUserAsync(client, email, password);

        var firstLogin = await client.PostAsJsonAsync("/api/auth/login", LoginRequestFactory.Valid(email, password));
        var firstLoginBody = await firstLogin.Content.ReadFromJsonAsync<AccessTokenResponse>();
        var secondLogin = await client.PostAsJsonAsync("/api/auth/login", LoginRequestFactory.Valid(email, password));
        var secondLoginBody = await secondLogin.Content.ReadFromJsonAsync<AccessTokenResponse>();

        Assert.Equal(HttpStatusCode.OK, firstLogin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondLogin.StatusCode);
        Assert.NotNull(firstLoginBody);
        Assert.NotNull(secondLoginBody);
        Assert.NotEqual(firstLoginBody!.Token, secondLoginBody!.Token);
    }

    [Fact]
    public async Task Refresh_WithValidCookie_ReturnsNewAccessToken()
    {
        using var client = _factory.CreateClient();

        var email = LoginRequestFactory.CreateEmail("auth-refresh");
        var password = "StrongPass123!";
        await CreateUserAsync(client, email, password);

        var login = await client.PostAsJsonAsync("/api/auth/login", LoginRequestFactory.Valid(email, password));
        var loginBody = await login.Content.ReadFromJsonAsync<AccessTokenResponse>();

        var refresh = await client.PostAsync("/api/auth/refresh", content: null);
        var refreshBody = await refresh.Content.ReadFromJsonAsync<AccessTokenResponse>();

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.NotNull(refreshBody);
        Assert.NotEqual(loginBody!.Token, refreshBody!.Token);
    }

    [Fact]
    public async Task Logout_CurrentSessionOnly_InvalidatesCurrentAccessToken_ButKeepsParallelSessionValid()
    {
        using var client1 = _factory.CreateClient();
        using var client2 = _factory.CreateClient();

        var email = LoginRequestFactory.CreateEmail("auth-logout");
        var password = "StrongPass123!";
        await CreateUserAsync(client1, email, password);

        var token1 = await LoginAndGetAccessTokenAsync(client1, email, password);
        var token2 = await LoginAndGetAccessTokenAsync(client2, email, password);

        var logoutResponse = await SendAuthorizedAsync(client1, HttpMethod.Post, "/api/auth/logout", token1);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var meWithToken1 = await SendAuthorizedAsync(client1, HttpMethod.Get, "/api/auth/me", token1);
        var meWithToken2 = await SendAuthorizedAsync(client2, HttpMethod.Get, "/api/auth/me", token2);

        Assert.Equal(HttpStatusCode.Unauthorized, meWithToken1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, meWithToken2.StatusCode);
    }

    [Fact]
    public async Task LogoutAll_InvalidatesAllActiveSessionsImmediately()
    {
        using var client1 = _factory.CreateClient();
        using var client2 = _factory.CreateClient();

        var email = LoginRequestFactory.CreateEmail("auth-logout-all");
        var password = "StrongPass123!";
        await CreateUserAsync(client1, email, password);

        var token1 = await LoginAndGetAccessTokenAsync(client1, email, password);
        var token2 = await LoginAndGetAccessTokenAsync(client2, email, password);

        var logoutAllResponse = await SendAuthorizedAsync(client1, HttpMethod.Post, "/api/auth/logout-all", token1);
        Assert.Equal(HttpStatusCode.NoContent, logoutAllResponse.StatusCode);

        var meWithToken1 = await SendAuthorizedAsync(client1, HttpMethod.Get, "/api/auth/me", token1);
        var meWithToken2 = await SendAuthorizedAsync(client2, HttpMethod.Get, "/api/auth/me", token2);

        Assert.Equal(HttpStatusCode.Unauthorized, meWithToken1.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, meWithToken2.StatusCode);
    }

    private static async Task CreateUserAsync(HttpClient client, string email, string password)
    {
        var request = new CreateUserRequest(email, password);
        var response = await client.PostAsJsonAsync("/api/users", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<string> LoginAndGetAccessTokenAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", LoginRequestFactory.Valid(email, password));
        var body = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);

        return body!.Token;
    }

    private static Task<HttpResponseMessage> SendAuthorizedAsync(HttpClient client, HttpMethod method, string url, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client.SendAsync(request);
    }
}
