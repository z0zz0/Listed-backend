using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Contracts.Signup;
using Listed.Application.Users.Common;
using Listed.Application.Users.Errors;
using Listed.Application.Users.Results;
using Microsoft.Extensions.Logging;

namespace Listed.Application.Users.Commands.SaveSignupPersonalInfo;

public sealed class SaveSignupPersonalInfoCommandHandler(
    ISignupVerificationStore signupVerificationStore,
    ILogger<SaveSignupPersonalInfoCommandHandler> logger) : ICommandHandler<SaveSignupPersonalInfoCommand, Result<SaveSignupPersonalInfoResult>>
{
    public async Task<Result<SaveSignupPersonalInfoResult>> Handle(SaveSignupPersonalInfoCommand command, CancellationToken cancellationToken)
    {
        if (command.SignupId == Guid.Empty)
        {
            return Result<SaveSignupPersonalInfoResult>.Failure(UserError.SignupStateMissing());
        }

        var firstNameResult = UserUtils.NormalizeAndValidateFirstName(command.FirstName);
        if (firstNameResult.IsFailure)
        {
            return Result<SaveSignupPersonalInfoResult>.Failure(firstNameResult.Error);
        }

        var lastNameResult = UserUtils.NormalizeAndValidateLastName(command.LastName);
        if (lastNameResult.IsFailure)
        {
            return Result<SaveSignupPersonalInfoResult>.Failure(lastNameResult.Error);
        }

        var dateOfBirthResult = UserUtils.ValidateDateOfBirth(command.DateOfBirth);
        if (dateOfBirthResult.IsFailure)
        {
            return Result<SaveSignupPersonalInfoResult>.Failure(dateOfBirthResult.Error);
        }

        var now = DateTime.UtcNow;
        var state = await signupVerificationStore.GetBySignupIdAsync(command.SignupId, cancellationToken);
        var stateValidationResult = SignupUtils.ValidateVerifiedSignupState(state, now);
        if (stateValidationResult.IsFailure)
        {
            return Result<SaveSignupPersonalInfoResult>.Failure(stateValidationResult.Error);
        }

        var verifiedState = stateValidationResult.Value!;
        var updatedState = verifiedState with
        {
            FirstName = firstNameResult.Value!,
            LastName = lastNameResult.Value!,
            DateOfBirth = dateOfBirthResult.Value!
        };

        var ttl = verifiedState.ExpiresAtUtc - now;
        await signupVerificationStore.SetAsync(command.SignupId, updatedState, ttl, cancellationToken);

        logger.LogInformation(
            "Signup personal info saved. SignupId={SignupId}, Email={Email}, HasDateOfBirth={HasDateOfBirth}",
            command.SignupId,
            verifiedState.Email,
            updatedState.DateOfBirth is not null);

        return Result<SaveSignupPersonalInfoResult>.Success(new SaveSignupPersonalInfoResult(command.SignupId));
    }
}
