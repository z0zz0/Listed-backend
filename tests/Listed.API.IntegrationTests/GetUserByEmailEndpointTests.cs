using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Listed.API.Contracts.Auth;
using Listed.Testing.Factories;

namespace Listed.API.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class GetUserByEmailEndpointTests : IClassFixture<ApiWebApplicationFactory>, IAsyncLifetime
{
    private readonly ApiWebApplicationFactory _factory;

    public GetUserByEmailEndpointTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetUsersByEmail_WithExistingUser_ReturnsOkWithExpandedPayload()
    {
        using var client = _factory.CreateClient();
        var authToken = await CreateAuthenticatedUserAndGetTokenAsync(client);

        var createPayload = CreateUserRequestFactory.Valid(email: "api.get@test.io");
        var createResponse = await client.PostAsJsonAsync("/api/users", createPayload);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var response = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/users/by-email?email=api.get@test.io", authToken);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("id", out var idElement));
        Assert.True(Guid.TryParse(idElement.GetString(), out _));
        Assert.Equal("api.get@test.io", root.GetProperty("email").GetString());
        Assert.False(root.GetProperty("isSoftDeleted").GetBoolean());
        Assert.False(root.GetProperty("isVerified").GetBoolean());

        Assert.True(root.TryGetProperty("userInfo", out var userInfoElement));
        Assert.Equal(JsonValueKind.Null, userInfoElement.ValueKind);

        Assert.True(root.TryGetProperty("photos", out var photosElement));
        Assert.Equal(JsonValueKind.Array, photosElement.ValueKind);
        Assert.Equal(0, photosElement.GetArrayLength());
    }

    [Fact]
    public async Task GetUsersByEmail_WithInvalidEmail_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var authToken = await CreateAuthenticatedUserAndGetTokenAsync(client);

        var response = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/users/by-email?email=invalid-email", authToken);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(body);
        Assert.Equal("User.Validation.InvalidEmail", document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetUsersByEmail_WithoutEmailQueryParam_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var authToken = await CreateAuthenticatedUserAndGetTokenAsync(client);

        var response = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/users/by-email", authToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUsersByEmail_WithUnknownEmail_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var authToken = await CreateAuthenticatedUserAndGetTokenAsync(client);

        var response = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/users/by-email?email=missing@test.io", authToken);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var document = JsonDocument.Parse(body);
        Assert.Equal("User.NotFound.ByEmail", document.RootElement.GetProperty("code").GetString());
    }

    private static async Task<string> CreateAuthenticatedUserAndGetTokenAsync(HttpClient client)
    {
        var email = CreateUserRequestFactory.CreateEmail("lookup-auth");
        var password = "StrongPass123!";

        var createResponse = await client.PostAsJsonAsync("/api/users", CreateUserRequestFactory.Valid(email, password));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", LoginRequestFactory.Valid(email, password));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.NotNull(loginBody);

        return loginBody!.Token;
    }

    private static Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client.SendAsync(request);
    }
}
