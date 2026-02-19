using Listed.Application.Contracts.Persistence;
using Listed.Application.Contracts.Security;
using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Users.Common;
using Listed.Application.Users.Errors;
using Listed.Application.Users.Results;
using Listed.Domain.Entities;
using Listed.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Listed.Application.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ILogger<CreateUserCommandHandler> logger) : ICommandHandler<CreateUserCommand, Result<CreateUserResult>>
{
    private const int MinPasswordLength = 8;

    public async Task<Result<CreateUserResult>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var validationResult = ValidateCreateUserCommand(command);
        if (validationResult.IsFailure)
        {
            logger.LogWarning(
                "CreateUser validation failed with error code {ErrorCode}. Email={Email}",
                validationResult.Error.Code,
                command.Email);

            return Result<CreateUserResult>.Failure(validationResult.Error);
        }

        var normalizedEmail = validationResult.Value!;

        if (await userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            logger.LogInformation(
                "CreateUser rejected because email already exists. Email={Email}",
                normalizedEmail);

            return Result<CreateUserResult>.Failure(UserError.EmailAlreadyInUse(normalizedEmail));
        }

        try
        {
            var passwordHash = passwordHasher.Hash(command.Password);
            var user = new User(normalizedEmail, passwordHash, passwordHasher.AlgorithmName);

            await userRepository.AddAsync(user, cancellationToken);

            logger.LogInformation(
                "CreateUser succeeded. UserId={UserId}, Email={Email}, Algorithm={Algorithm}",
                user.Id,
                user.Email,
                user.PasswordAlgorithm);

            return Result<CreateUserResult>.Success(new CreateUserResult(user.Id, user.Email));
        }
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintCode == PersistenceConstraintCodes.User.EmailUnique)
        {
            logger.LogInformation(
                "CreateUser hit unique constraint conflict. ConstraintCode={ConstraintCode}, ConstraintName={ConstraintName}, Email={Email}",
                ex.ConstraintCode,
                ex.ConstraintName,
                normalizedEmail);

            return Result<CreateUserResult>.Failure(UserError.EmailAlreadyInUse(normalizedEmail));
        }
        catch (UserDomainException ex)
        {
            logger.LogWarning(
                ex,
                "CreateUser failed with domain validation error for Email={Email}",
                normalizedEmail);

            return Result<CreateUserResult>.Failure(UserError.InvalidUserData(ex.Message));
        }
    }

    private static Result<string> ValidateCreateUserCommand(CreateUserCommand command)
    {
        var emailResult = UserUtils.NormalizeAndValidateEmail(command.Email);
        if (emailResult.IsFailure)
        {
            return Result<string>.Failure(emailResult.Error);
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return Result<string>.Failure(UserError.InvalidPassword());
        }
        
        if (command.Password.Length < MinPasswordLength)
        {
            return Result<string>.Failure(UserError.InvalidPasswordTooShort(MinPasswordLength));
        }

        return emailResult;
    }
}
