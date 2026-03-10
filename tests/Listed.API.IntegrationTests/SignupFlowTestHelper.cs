using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Listed.API.Contracts.Users;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Listed.API.IntegrationTests;

internal static partial class SignupFlowTestHelper
{
    public static async Task<(HttpResponseMessage Response, CompleteSignupResponse Body)> CompleteSignupThroughFlowAsync(
        ApiWebApplicationFactory factory,
        HttpClient client,
        string email,
        string password,
        string firstName = "John",
        string lastName = "Doe",
        DateOnly? dateOfBirth = null)
    {
        var (signupId, normalizedEmail) = await CreateReadyForCompletionSignupAsync(
            factory,
            client,
            email,
            firstName,
            lastName,
            dateOfBirth);

        var completeResponse = await client.PostAsJsonAsync(
            "/api/users/signup/complete",
            new CompleteSignupRequest(signupId, password));
        var completeBody = await completeResponse.Content.ReadFromJsonAsync<CompleteSignupResponse>();

        Assert.Equal(HttpStatusCode.Created, completeResponse.StatusCode);
        Assert.NotNull(completeBody);
        Assert.Equal(normalizedEmail, completeBody!.Email);

        return (completeResponse, completeBody);
    }

    public static async Task CreateUserThroughSignupAsync(
        ApiWebApplicationFactory factory,
        HttpClient client,
        string email,
        string password,
        string firstName = "John",
        string lastName = "Doe",
        DateOnly? dateOfBirth = null)
    {
        _ = await CompleteSignupThroughFlowAsync(
            factory,
            client,
            email,
            password,
            firstName,
            lastName,
            dateOfBirth);
    }

    public static async Task<(Guid SignupId, string NormalizedEmail)> CreateReadyForCompletionSignupAsync(
        ApiWebApplicationFactory factory,
        HttpClient client,
        string email,
        string firstName = "John",
        string lastName = "Doe",
        DateOnly? dateOfBirth = null)
    {
        var startResponse = await client.PostAsJsonAsync("/api/users/signup/start", new StartSignupRequest(email));
        var startBody = await startResponse.Content.ReadFromJsonAsync<StartSignupResponse>();

        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        Assert.NotNull(startBody);

        var verificationCode = GetLatestVerificationCode(factory, startBody!.Email);

        var verifyResponse = await client.PostAsJsonAsync(
            "/api/users/signup/verify-code",
            new VerifySignupEmailRequest(startBody.SignupId, verificationCode));
        var verifyBody = await verifyResponse.Content.ReadFromJsonAsync<VerifySignupEmailResponse>();

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        Assert.NotNull(verifyBody);
        Assert.Equal(startBody.SignupId, verifyBody!.SignupId);

        var savePersonalInfoResponse = await client.PostAsJsonAsync(
            "/api/users/signup/personal-info",
            new SaveSignupPersonalInfoRequest(
                startBody.SignupId,
                firstName,
                lastName,
                dateOfBirth ?? new DateOnly(1990, 1, 1)));
        var savePersonalInfoBody = await savePersonalInfoResponse.Content.ReadFromJsonAsync<SaveSignupPersonalInfoResponse>();

        Assert.Equal(HttpStatusCode.OK, savePersonalInfoResponse.StatusCode);
        Assert.NotNull(savePersonalInfoBody);
        Assert.Equal(startBody.SignupId, savePersonalInfoBody!.SignupId);

        return (startBody.SignupId, startBody.Email);
    }

    public static string GetLatestVerificationCode(ApiWebApplicationFactory factory, string toAddress)
    {
        var emailSender = factory.Services.GetRequiredService<InMemoryEmailSender>();
        var message = emailSender.GetLatestMessage(toAddress);

        Assert.NotNull(message);

        var match = VerificationCodeRegex().Match(message!.Body);
        Assert.True(match.Success);

        return match.Groups[1].Value;
    }

    [GeneratedRegex(@"\b(\d{6})\b", RegexOptions.Compiled)]
    private static partial Regex VerificationCodeRegex();
}
