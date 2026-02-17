using Listed.Application.Contracts.Persistence;
using Listed.Application.Contracts.Security;
using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Errors;
using Listed.Domain.Exceptions;
using Listed.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Listed.Application.Commands.CreateUser;

public sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ILogger<CreateUserCommandHandler> logger) : ICommandHandler<CreateUserCommand, Result<Guid>>
{
    private const int MinPasswordLength = 8;

    public async Task<Result<Guid>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var validationResult = ValidateCreateUserCommand(command);
        if (validationResult.IsFailure)
        {
            logger.LogWarning(
                "CreateUser validation failed with error code {ErrorCode}",
                validationResult.Error.Code);

            return Result<Guid>.Failure(validationResult.Error);
        }

        var normalizedEmail = validationResult.Value!;

        if (await userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            logger.LogInformation(
                "CreateUser rejected because email already exists. Email={Email}",
                normalizedEmail);

            return Result<Guid>.Failure(UserError.EmailAlreadyInUse(normalizedEmail));
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

            return Result<Guid>.Success(user.Id);
        }
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintCode == PersistenceConstraintCodes.UserEmailUnique)
        {
            logger.LogInformation(
                "CreateUser hit unique constraint conflict. ConstraintCode={ConstraintCode}, ConstraintName={ConstraintName}, Email={Email}",
                ex.ConstraintCode,
                ex.ConstraintName,
                normalizedEmail);

            return Result<Guid>.Failure(UserError.EmailAlreadyInUse(normalizedEmail));
        }
        catch (UserDomainException ex)
        {
            logger.LogWarning(
                ex,
                "CreateUser failed with domain validation error for Email={Email}",
                normalizedEmail);

            return Result<Guid>.Failure(UserError.InvalidUserData(ex.Message));
        }
    }

    private static Result<string> ValidateCreateUserCommand(CreateUserCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return Result<string>.Failure(UserError.InvalidEmail());
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return Result<string>.Failure(UserError.InvalidPassword());
        }

        var normalizedEmail = command.Email.Trim().ToLowerInvariant();

        if (!normalizedEmail.Contains('@'))
        {
            return Result<string>.Failure(UserError.InvalidEmail());
        }

        if (command.Password.Length < MinPasswordLength)
        {
            return Result<string>.Failure(UserError.InvalidPasswordTooShort(MinPasswordLength));
        }

        return Result<string>.Success(normalizedEmail);
    }
}
