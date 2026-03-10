using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Contracts.Signup;
using Listed.Application.Users.Common;
using Listed.Application.Users.Errors;
using Listed.Application.Users.Results;
using Microsoft.Extensions.Logging;

namespace Listed.Application.Users.Commands.VerifySignupEmail;

public sealed class VerifySignupEmailCommandHandler(
    ISignupVerificationStore signupVerificationStore,
    ILogger<VerifySignupEmailCommandHandler> logger) : ICommandHandler<VerifySignupEmailCommand, Result<VerifySignupEmailResult>>
{
    public async Task<Result<VerifySignupEmailResult>> Handle(VerifySignupEmailCommand command, CancellationToken cancellationToken)
    {
        if (command.SignupId == Guid.Empty)
        {
            return Result<VerifySignupEmailResult>.Failure(UserError.SignupStateMissing());
        }

        var codeResult = SignupUtils.NormalizeAndValidateVerificationCode(command.VerificationCode);
        if (codeResult.IsFailure)
        {
            logger.LogInformation("VerifySignupEmail failed due to invalid code format. SignupId={SignupId}", command.SignupId);
            return Result<VerifySignupEmailResult>.Failure(codeResult.Error);
        }

        var normalizedCode = codeResult.Value!;
        var now = DateTime.UtcNow;
        var state = await signupVerificationStore.GetBySignupIdAsync(command.SignupId, cancellationToken);

        if (state is null)
        {
            logger.LogInformation("VerifySignupEmail failed because no verification state exists. SignupId={SignupId}", command.SignupId);
            return Result<VerifySignupEmailResult>.Failure(UserError.InvalidVerificationCode());
        }

        if (state.IsVerified)
        {
            var verifiedAtUtc = state.VerifiedAtUtc ?? now;
            return Result<VerifySignupEmailResult>.Success(new VerifySignupEmailResult(command.SignupId, verifiedAtUtc));
        }

        if (state.ExpiresAtUtc <= now)
        {
            logger.LogInformation(
                "VerifySignupEmail failed because verification code expired. SignupId={SignupId}, Email={Email}, ExpiresAtUtc={ExpiresAtUtc}",
                command.SignupId,
                state.Email,
                state.ExpiresAtUtc);

            return Result<VerifySignupEmailResult>.Failure(UserError.VerificationCodeExpired());
        }

        if (state.FailedAttempts >= SignupUtils.MaxVerificationAttempts)
        {
            logger.LogInformation(
                "VerifySignupEmail blocked due to max attempts reached. SignupId={SignupId}, Email={Email}, FailedAttempts={FailedAttempts}",
                command.SignupId,
                state.Email,
                state.FailedAttempts);

            return Result<VerifySignupEmailResult>.Failure(UserError.VerificationAttemptsExceeded(SignupUtils.MaxVerificationAttempts));
        }

        if (!SignupUtils.DoesVerificationCodeHashMatch(normalizedCode, state.CodeHash))
        {
            return await HandleInvalidVerificationCodeAsync(command.SignupId, state, now, cancellationToken);
        }

        var verifiedAt = now;
        var verifiedState = state with
        {
            FailedAttempts = 0,
            IsVerified = true,
            VerifiedAtUtc = verifiedAt,
            CodeHash = string.Empty,
            ExpiresAtUtc = verifiedAt.Add(SignupUtils.VerifiedStateLifetime)
        };

        await signupVerificationStore.SetAsync(command.SignupId, verifiedState, SignupUtils.VerifiedStateLifetime, cancellationToken);

        logger.LogInformation(
            "VerifySignupEmail succeeded. SignupId={SignupId}, Email={Email}, VerifiedAtUtc={VerifiedAtUtc}",
            command.SignupId,
            state.Email,
            verifiedAt);

        return Result<VerifySignupEmailResult>.Success(new VerifySignupEmailResult(command.SignupId, verifiedAt));
    }

    private async Task<Result<VerifySignupEmailResult>> HandleInvalidVerificationCodeAsync(
        Guid signupId,
        SignupVerificationState state,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var updatedFailedAttempts = state.FailedAttempts + 1;
        var updatedState = state with { FailedAttempts = updatedFailedAttempts };
        var remainingTtl = state.ExpiresAtUtc - now;

        await signupVerificationStore.SetAsync(signupId, updatedState, remainingTtl, cancellationToken);

        if (updatedFailedAttempts >= SignupUtils.MaxVerificationAttempts)
        {
            logger.LogInformation(
                "VerifySignupEmail reached max failed attempts. SignupId={SignupId}, Email={Email}, FailedAttempts={FailedAttempts}",
                signupId,
                state.Email,
                updatedFailedAttempts);

            return Result<VerifySignupEmailResult>.Failure(UserError.VerificationAttemptsExceeded(SignupUtils.MaxVerificationAttempts));
        }

        logger.LogInformation(
            "VerifySignupEmail failed due to invalid verification code. SignupId={SignupId}, Email={Email}, FailedAttempts={FailedAttempts}",
            signupId,
            state.Email,
            updatedFailedAttempts);

        return Result<VerifySignupEmailResult>.Failure(UserError.InvalidVerificationCode());
    }
}
