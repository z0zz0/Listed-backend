using Listed.Application.Common;
using Listed.Application.Contracts.Communication;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Contracts.Persistence;
using Listed.Application.Contracts.Signup;
using Listed.Application.Users.Common;
using Listed.Application.Users.Errors;
using Listed.Application.Users.Results;
using Microsoft.Extensions.Logging;

namespace Listed.Application.Users.Commands.StartSignup;

public sealed class StartSignupCommandHandler(
    IUserRepository userRepository,
    ISignupVerificationStore signupVerificationStore,
    IEmailSender emailSender,
    ILogger<StartSignupCommandHandler> logger) : ICommandHandler<StartSignupCommand, Result<StartSignupResult>>
{
    public async Task<Result<StartSignupResult>> Handle(StartSignupCommand command, CancellationToken cancellationToken)
    {
        var emailResult = UserUtils.NormalizeAndValidateEmail(command.Email);
        if (emailResult.IsFailure)
        {
            logger.LogWarning(
                "StartSignup validation failed with error code {ErrorCode}. Email={Email}",
                emailResult.Error.Code,
                command.Email);

            return Result<StartSignupResult>.Failure(emailResult.Error);
        }

        var normalizedEmail = emailResult.Value!;

        if (await userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            logger.LogInformation(
                "StartSignup rejected because email already exists. Email={Email}",
                normalizedEmail);

            return Result<StartSignupResult>.Failure(UserError.EmailAlreadyInUse(normalizedEmail));
        }

        var now = DateTime.UtcNow;
        var signupId = Guid.NewGuid();
        var expiresAtUtc = now.Add(SignupUtils.VerificationCodeLifetime);
        var verificationCode = SignupUtils.GenerateVerificationCode();
        var verificationCodeHash = SignupUtils.HashVerificationCode(verificationCode);

        var state = new SignupVerificationState(
            normalizedEmail,
            verificationCodeHash,
            expiresAtUtc,
            FailedAttempts: 0,
            IsVerified: false,
            VerifiedAtUtc: null);

        await signupVerificationStore.SetAsync(signupId, state, SignupUtils.VerificationCodeLifetime, cancellationToken);

        try
        {
            await emailSender.SendAsync(
                new EmailMessage(
                    normalizedEmail,
                    "Your Listed verification code",
                    BuildVerificationEmailBody(verificationCode)),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "StartSignup failed while sending verification email. Email={Email}", normalizedEmail);
            return Result<StartSignupResult>.Failure(UserError.SignupEmailDeliveryFailed());
        }

        logger.LogInformation(
            "StartSignup verification code issued. SignupId={SignupId}, Email={Email}, ExpiresAtUtc={ExpiresAtUtc}",
            signupId,
            normalizedEmail,
            expiresAtUtc);

        return Result<StartSignupResult>.Success(new StartSignupResult(signupId, normalizedEmail, expiresAtUtc));
    }

    private static string BuildVerificationEmailBody(string verificationCode)
    {
        return $"Your Listed verification code is: {verificationCode}. It expires in {SignupUtils.VerificationCodeLifetime.TotalMinutes:0} minutes.";
    }
}
