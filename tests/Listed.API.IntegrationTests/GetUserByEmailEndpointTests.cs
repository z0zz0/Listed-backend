using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

        var createPayload = CreateUserRequestFactory.Valid(email: "api.get@test.io");
        var createResponse = await client.PostAsJsonAsync("/api/users", createPayload);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var response = await client.GetAsync("/api/users/by-email?email=api.get@test.io");
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

        var response = await client.GetAsync("/api/users/by-email?email=invalid-email");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(body);
        Assert.Equal("User.Validation.InvalidEmail", document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetUsersByEmail_WithoutEmailQueryParam_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users/by-email");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUsersByEmail_WithUnknownEmail_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users/by-email?email=missing@test.io");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var document = JsonDocument.Parse(body);
        Assert.Equal("User.NotFound.ByEmail", document.RootElement.GetProperty("code").GetString());
    }
}
