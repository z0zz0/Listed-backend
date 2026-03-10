using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Listed.API.Contracts.Auth;
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
        _factory.ResetEmailInbox();
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

        var sessionWithToken1 = await SendAuthorizedAsync(client1, HttpMethod.Get, "/api/auth/session", token1);
        var sessionWithToken2 = await SendAuthorizedAsync(client2, HttpMethod.Get, "/api/auth/session", token2);

        Assert.Equal(HttpStatusCode.Unauthorized, sessionWithToken1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sessionWithToken2.StatusCode);
    }

    [Fact]
    public async Task Logout_InvalidatesAllAccessTokensInSameSessionImmediately()
    {
        using var client = _factory.CreateClient();

        var email = LoginRequestFactory.CreateEmail("auth-logout-session");
        var password = "StrongPass123!";
        await CreateUserAsync(client, email, password);

        var login = await client.PostAsJsonAsync("/api/auth/login", LoginRequestFactory.Valid(email, password));
        var loginBody = await login.Content.ReadFromJsonAsync<AccessTokenResponse>();
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.NotNull(loginBody);

        var refresh = await client.PostAsync("/api/auth/refresh", content: null);
        var refreshBody = await refresh.Content.ReadFromJsonAsync<AccessTokenResponse>();
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.NotNull(refreshBody);
        Assert.NotEqual(loginBody!.Token, refreshBody!.Token);

        var logoutResponse = await SendAuthorizedAsync(client, HttpMethod.Post, "/api/auth/logout", loginBody.Token);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var sessionWithRefreshedToken = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/auth/session", refreshBody.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, sessionWithRefreshedToken.StatusCode);
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

        var sessionWithToken1 = await SendAuthorizedAsync(client1, HttpMethod.Get, "/api/auth/session", token1);
        var sessionWithToken2 = await SendAuthorizedAsync(client2, HttpMethod.Get, "/api/auth/session", token2);

        Assert.Equal(HttpStatusCode.Unauthorized, sessionWithToken1.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, sessionWithToken2.StatusCode);
    }

    [Fact]
    public async Task LogoutAll_WithStolenTokenFromAnotherDevice_ReturnsUnauthorized_AndKeepsSessionsValid()
    {
        using var browserClient = _factory.CreateClient();
        using var brunoClient = _factory.CreateClient();

        var email = LoginRequestFactory.CreateEmail("auth-logout-all-stolen-cross-device");
        var password = "StrongPass123!";
        await CreateUserAsync(browserClient, email, password);

        var browserToken = await LoginAndGetAccessTokenAsync(browserClient, email, password);
        var brunoToken = await LoginAndGetAccessTokenAsync(brunoClient, email, password);

        var logoutAllWithStolenToken = await SendAuthorizedAsync(brunoClient, HttpMethod.Post, "/api/auth/logout-all", browserToken);
        Assert.Equal(HttpStatusCode.Unauthorized, logoutAllWithStolenToken.StatusCode);

        var browserSessionAfterAttempt = await SendAuthorizedAsync(browserClient, HttpMethod.Get, "/api/auth/session", browserToken);
        var brunoSessionAfterAttempt = await SendAuthorizedAsync(brunoClient, HttpMethod.Get, "/api/auth/session", brunoToken);

        Assert.Equal(HttpStatusCode.OK, browserSessionAfterAttempt.StatusCode);
        Assert.Equal(HttpStatusCode.OK, brunoSessionAfterAttempt.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutRefreshCookie_ReturnsUnauthorized_AndDoesNotInvalidateExistingSession()
    {
        using var browserClient = _factory.CreateClient();
        using var brunoClient = _factory.CreateClient();

        var email = LoginRequestFactory.CreateEmail("auth-logout-cross-client");
        var password = "StrongPass123!";
        await CreateUserAsync(browserClient, email, password);

        var firstLoginResponse = await browserClient.PostAsJsonAsync("/api/auth/login", LoginRequestFactory.Valid(email, password));
        var firstLoginBody = await firstLoginResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();
        Assert.Equal(HttpStatusCode.OK, firstLoginResponse.StatusCode);
        Assert.NotNull(firstLoginBody);

        var logoutResponse = await SendAuthorizedAsync(brunoClient, HttpMethod.Post, "/api/auth/logout", firstLoginBody!.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, logoutResponse.StatusCode);

        var browserSessionWithOriginalToken = await SendAuthorizedAsync(browserClient, HttpMethod.Get, "/api/auth/session", firstLoginBody.Token);
        Assert.Equal(HttpStatusCode.OK, browserSessionWithOriginalToken.StatusCode);

        var secondLoginResponse = await browserClient.PostAsJsonAsync("/api/auth/login", LoginRequestFactory.Valid(email, password));
        var secondLoginBody = await secondLoginResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();
        Assert.Equal(HttpStatusCode.OK, secondLoginResponse.StatusCode);
        Assert.NotNull(secondLoginBody);

        var sessionResponse = await SendAuthorizedAsync(browserClient, HttpMethod.Get, "/api/auth/session", secondLoginBody!.Token);
        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_WithStolenTokenFromAnotherDevice_DoesNotRevokeOtherDeviceRefreshSession()
    {
        using var browserClient = _factory.CreateClient();
        using var brunoClient = _factory.CreateClient();

        var email = LoginRequestFactory.CreateEmail("auth-logout-stolen-cross-device");
        var password = "StrongPass123!";
        await CreateUserAsync(browserClient, email, password);

        var browserLogin = await browserClient.PostAsJsonAsync("/api/auth/login", LoginRequestFactory.Valid(email, password));
        var browserLoginBody = await browserLogin.Content.ReadFromJsonAsync<AccessTokenResponse>();
        Assert.Equal(HttpStatusCode.OK, browserLogin.StatusCode);
        Assert.NotNull(browserLoginBody);

        var brunoLogin = await brunoClient.PostAsJsonAsync("/api/auth/login", LoginRequestFactory.Valid(email, password));
        var brunoLoginBody = await brunoLogin.Content.ReadFromJsonAsync<AccessTokenResponse>();
        Assert.Equal(HttpStatusCode.OK, brunoLogin.StatusCode);
        Assert.NotNull(brunoLoginBody);

        var logoutWithStolenToken = await SendAuthorizedAsync(brunoClient, HttpMethod.Post, "/api/auth/logout", browserLoginBody!.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, logoutWithStolenToken.StatusCode);

        var browserSessionAfterLogout = await SendAuthorizedAsync(browserClient, HttpMethod.Get, "/api/auth/session", browserLoginBody.Token);
        Assert.Equal(HttpStatusCode.OK, browserSessionAfterLogout.StatusCode);

        var brunoSessionAfterLogout = await SendAuthorizedAsync(brunoClient, HttpMethod.Get, "/api/auth/session", brunoLoginBody!.Token);
        Assert.Equal(HttpStatusCode.OK, brunoSessionAfterLogout.StatusCode);

        var brunoRefreshAfterCrossDeviceLogout = await brunoClient.PostAsync("/api/auth/refresh", content: null);
        Assert.Equal(HttpStatusCode.OK, brunoRefreshAfterCrossDeviceLogout.StatusCode);
    }

    private async Task CreateUserAsync(HttpClient client, string email, string password)
    {
        await SignupFlowTestHelper.CreateUserThroughSignupAsync(_factory, client, email, password);
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
