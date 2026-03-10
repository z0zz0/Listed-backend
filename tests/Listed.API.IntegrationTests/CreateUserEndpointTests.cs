using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Listed.API.Contracts.Auth;
using Listed.API.Contracts.Users;
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
        _factory.ResetEmailInbox();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PostUsers_WithValidPayload_ReturnsCreated_AndPersistsUser()
    {
        using var client = _factory.CreateClient();
        var email = LoginRequestFactory.CreateEmail("ok");
        var password = "StrongPass123!";
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var (response, completeBody) = await SignupFlowTestHelper.CompleteSignupThroughFlowAsync(
            _factory,
            client,
            email,
            password);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        Assert.False(string.IsNullOrWhiteSpace(completeBody.AccessToken.Token));

        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("id", out var idElement));
        Assert.True(Guid.TryParse(idElement.GetString(), out _));
        Assert.Equal(normalizedEmail, document.RootElement.GetProperty("email").GetString());

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ListedDbContext>();
        var savedUser = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == normalizedEmail);
        Assert.NotNull(savedUser);
        Assert.Equal("bcrypt", savedUser.PasswordAlgorithm);
    }

    [Fact]
    public async Task PostUsers_WithInvalidEmail_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/signup/start", new StartSignupRequest("invalid-email"));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(body);
        Assert.Equal("User.Validation.InvalidEmail", document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task PostUsers_WithShortPassword_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var (signupId, _) = await SignupFlowTestHelper.CreateReadyForCompletionSignupAsync(
            _factory,
            client,
            LoginRequestFactory.CreateEmail("short"));

        var response = await client.PostAsJsonAsync("/api/users/signup/complete", new CompleteSignupRequest(signupId, "1234567"));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(body);
        Assert.Equal("User.Validation.PasswordTooShort", document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task PostUsers_WithDuplicateEmail_ReturnsConflict()
    {
        using var client = _factory.CreateClient();
        var email = LoginRequestFactory.CreateEmail("dup");

        await SignupFlowTestHelper.CreateUserThroughSignupAsync(_factory, client, email, "StrongPass123!");

        var secondResponse = await client.PostAsJsonAsync("/api/users/signup/start", new StartSignupRequest(email));
        var body = await secondResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        using var document = JsonDocument.Parse(body);
        Assert.Equal("User.Conflict.EmailAlreadyInUse", document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task PostUsers_NormalizesEmail_AndSupportsLoginByNormalizedEmail()
    {
        using var client = _factory.CreateClient();
        var email = "  Mixed.Case@Test.IO  ";
        var password = "StrongPass123!";
        var normalizedEmail = email.Trim().ToLowerInvariant();

        await SignupFlowTestHelper.CreateUserThroughSignupAsync(_factory, client, email, password);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", LoginRequestFactory.Valid(normalizedEmail, password));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.NotNull(loginBody);
        Assert.False(string.IsNullOrWhiteSpace(loginBody!.Token));

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ListedDbContext>();
        var savedUser = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == normalizedEmail);
        Assert.NotNull(savedUser);
        Assert.Equal(normalizedEmail, savedUser!.Email);
    }
}
