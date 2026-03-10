using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Contracts.Persistence;
using Listed.Application.Contracts.Security;
using Listed.Application.Contracts.Signup;
using Listed.Application.Users.Common;
using Listed.Application.Users.Errors;
using Listed.Application.Users.Results;
using Listed.Domain.Entities;
using Listed.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Listed.Application.Users.Commands.CompleteSignup;

public sealed class CompleteSignupCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ISignupVerificationStore signupVerificationStore,
    ILogger<CompleteSignupCommandHandler> logger) : ICommandHandler<CompleteSignupCommand, Result<CompleteSignupResult>>
{
    public async Task<Result<CompleteSignupResult>> Handle(CompleteSignupCommand command, CancellationToken cancellationToken)
    {
        if (command.SignupId == Guid.Empty)
        {
            return Result<CompleteSignupResult>.Failure(UserError.SignupStateMissing());
        }

        var passwordResult = UserUtils.ValidatePassword(command.Password);
        if (passwordResult.IsFailure)
        {
            return Result<CompleteSignupResult>.Failure(passwordResult.Error);
        }

        var now = DateTime.UtcNow;
        var state = await signupVerificationStore.GetBySignupIdAsync(command.SignupId, cancellationToken);
        var stateValidationResult = SignupUtils.ValidateVerifiedSignupState(state, now);
        if (stateValidationResult.IsFailure)
        {
            return Result<CompleteSignupResult>.Failure(stateValidationResult.Error);
        }

        var verifiedState = stateValidationResult.Value!;
        var normalizedEmail = verifiedState.Email;

        if (await userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            logger.LogInformation(
                "CompleteSignup found existing account before create. Continuing with existing user. SignupId={SignupId}, Email={Email}",
                command.SignupId,
                normalizedEmail);

            return await ContinueWithExistingUserAsync(command.SignupId, verifiedState, normalizedEmail, cancellationToken);
        }

        if (!DoesSignupStateHaveRequiredPersonalInfoData(verifiedState))
        {
            return Result<CompleteSignupResult>.Failure(UserError.SignupPersonalInfoIncomplete());
        }

        try
        {
            var passwordHash = passwordHasher.Hash(passwordResult.Value!);
            var user = new User(normalizedEmail, passwordHash, passwordHasher.AlgorithmName);
            var userInfo = new UserInfo(user.Id, verifiedState.FirstName!, verifiedState.LastName!, verifiedState.DateOfBirth!.Value);

            user.SetUserInfo(userInfo);

            await userRepository.AddAsync(user, cancellationToken);
            await signupVerificationStore.SetAsync(command.SignupId, verifiedState, TimeSpan.Zero, cancellationToken);

            logger.LogInformation("Signup completed. SignupId={SignupId}, UserId={UserId}, Email={Email}", command.SignupId, user.Id, user.Email);

            return Result<CompleteSignupResult>.Success(new CompleteSignupResult(user.Id, user.Email));
        }
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintCode == PersistenceConstraintCodes.User.EmailUnique)
        {
            logger.LogInformation(
                "CompleteSignup hit unique constraint conflict. ConstraintCode={ConstraintCode}, ConstraintName={ConstraintName}, Email={Email}",
                ex.ConstraintCode,
                ex.ConstraintName,
                normalizedEmail);

            return await ContinueWithExistingUserAsync(command.SignupId, verifiedState, normalizedEmail, cancellationToken);
        }
        catch (UserDomainException ex)
        {
            logger.LogWarning(ex, "CompleteSignup failed with domain validation error for Email={Email}", normalizedEmail);
            return Result<CompleteSignupResult>.Failure(UserError.InvalidUserData(ex.Message));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "CompleteSignup failed with user-info validation error for Email={Email}", normalizedEmail);
            return Result<CompleteSignupResult>.Failure(UserError.InvalidUserData(ex.Message));
        }
    }

    private static bool DoesSignupStateHaveRequiredPersonalInfoData(SignupVerificationState signupState)
    {
        return !string.IsNullOrWhiteSpace(signupState.Email)
                && !string.IsNullOrWhiteSpace(signupState.FirstName)
                && !string.IsNullOrWhiteSpace(signupState.LastName)
                && signupState.DateOfBirth.HasValue;
    }

    private async Task<Result<CompleteSignupResult>> ContinueWithExistingUserAsync(
        Guid signupId,
        SignupVerificationState verifiedState,
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        var existingUser = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existingUser is null)
        {
            logger.LogWarning(
                "CompleteSignup expected existing user but none was found. SignupId={SignupId}, Email={Email}",
                signupId,
                normalizedEmail);

            return Result<CompleteSignupResult>.Failure(UserError.EmailAlreadyInUse(normalizedEmail));
        }

        await signupVerificationStore.SetAsync(signupId, verifiedState, TimeSpan.Zero, cancellationToken);

        logger.LogInformation(
            "CompleteSignup continued with existing account. SignupId={SignupId}, UserId={UserId}, Email={Email}",
            signupId,
            existingUser.Id,
            existingUser.Email);

        return Result<CompleteSignupResult>.Success(new CompleteSignupResult(existingUser.Id, existingUser.Email));
    }
}
