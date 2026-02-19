using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Listed.Infrastructure.Persistence;
using Listed.Testing.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Listed.API.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class CreateUserEndpointTests : IClassFixture<ApiWebApplicationFactory>, IAsyncLifetime
{
    private readonly ApiWebApplicationFactory _factory;

    public CreateUserEndpointTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PostUsers_WithValidPayload_ReturnsCreated_AndPersistsUser()
    {
        using var client = _factory.CreateClient();
        var payload = CreateUserRequestFactory.Valid();

        var response = await client.PostAsJsonAsync("/api/users", payload);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));

        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("id", out var idElement));
        Assert.True(Guid.TryParse(idElement.GetString(), out _));
        Assert.Equal(payload.Email.Trim().ToLowerInvariant(), document.RootElement.GetProperty("email").GetString());

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ListedDbContext>();
        var savedUser = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == payload.Email);
        Assert.NotNull(savedUser);
        Assert.Equal("bcrypt", savedUser.PasswordAlgorithm);
    }

    [Fact]
    public async Task PostUsers_WithInvalidEmail_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var payload = CreateUserRequestFactory.InvalidEmail();

        var response = await client.PostAsJsonAsync("/api/users", payload);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(body);
        Assert.Equal("User.Validation.InvalidEmail", document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task PostUsers_WithShortPassword_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var payload = CreateUserRequestFactory.ShortPassword();

        var response = await client.PostAsJsonAsync("/api/users", payload);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(body);
        Assert.Equal("User.Validation.PasswordTooShort", document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task PostUsers_WithDuplicateEmail_ReturnsConflict()
    {
        using var client = _factory.CreateClient();
        var payload = CreateUserRequestFactory.Valid(email: CreateUserRequestFactory.CreateEmail("dup"));

        var firstResponse = await client.PostAsJsonAsync("/api/users", payload);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync("/api/users", payload);
        var body = await secondResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        using var document = JsonDocument.Parse(body);
        Assert.Equal("User.Conflict.EmailAlreadyInUse", document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task PostUsers_NormalizesEmail_AndSupportsLookupByNormalizedEmail()
    {
        using var client = _factory.CreateClient();
        var payload = CreateUserRequestFactory.Valid(email: "  Mixed.Case@Test.IO  ");

        var createResponse = await client.PostAsJsonAsync("/api/users", payload);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var lookupResponse = await client.GetAsync("/api/users/by-email?email=mixed.case@test.io");
        var lookupBody = await lookupResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, lookupResponse.StatusCode);

        using var document = JsonDocument.Parse(lookupBody);
        Assert.Equal("mixed.case@test.io", document.RootElement.GetProperty("email").GetString());
    }
}
