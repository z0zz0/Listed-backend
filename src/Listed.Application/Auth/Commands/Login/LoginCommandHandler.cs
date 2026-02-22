using Listed.Application.Auth.Errors;
using Listed.Application.Auth.Results;
using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Contracts.Persistence;
using Listed.Application.Contracts.Security;
using Listed.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Listed.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IUserAuthRepository userAuthRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IAccessTokenService accessTokenService,
    IRefreshTokenService refreshTokenService,
    IAuthSettings authSettings,
    ILogger<LoginCommandHandler> logger) : ICommandHandler<LoginCommand, Result<AuthTokensResult>>
{
    private const int MaxRefreshTokenGenerationAttempts = 3;

    public async Task<Result<AuthTokensResult>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var userResult = await ValidateAndAuthenticateAsync(command, cancellationToken);
        if (userResult.IsFailure)
        {
            return Result<AuthTokensResult>.Failure(userResult.Error);
        }

        var user = userResult.Value!;

        var utcNow = DateTime.UtcNow;
        await refreshTokenRepository.RevokeExpiredByUserAndDeviceAsync(user.Id, command.DeviceId, utcNow, cancellationToken);

        var activeDeviceSession = await refreshTokenRepository.GetActiveByUserAndDeviceAsync(
            user.Id,
            command.DeviceId,
            utcNow,
            cancellationToken);

        if (activeDeviceSession is not null)
        {
            var idempotentReuseResult = TryReuseActiveDeviceSession(user, activeDeviceSession, command, utcNow);
            if (idempotentReuseResult.IsSuccess)
            {
                return Result<AuthTokensResult>.Success(idempotentReuseResult.Value!);
            }

            return Result<AuthTokensResult>.Failure(idempotentReuseResult.Error);
        }

        var accessToken = accessTokenService.Create(user.Id, user.Email, user.AuthInfo.AuthVersion, utcNow);
        var refreshCreationResult = await CreateRefreshTokenAsync(user, command.DeviceId, utcNow, command, cancellationToken);
        if (refreshCreationResult.IsFailure)
        {
            return Result<AuthTokensResult>.Failure(refreshCreationResult.Error);
        }

        var created = refreshCreationResult.Value!;

        logger.LogInformation("Login succeeded. UserId={UserId}, Email={Email}", user.Id, user.Email);

        return Result<AuthTokensResult>.Success(new AuthTokensResult(
            accessToken,
            created.PlainToken,
            created.Entity.ExpiresAt));
    }

    private async Task<Result<(RefreshToken Entity, string PlainToken)>> CreateRefreshTokenAsync(
        User user,
        Guid deviceId,
        DateTime utcNow,
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxRefreshTokenGenerationAttempts; attempt++)
        {
            var plainToken = refreshTokenService.GenerateToken();
            var tokenHash = refreshTokenService.HashToken(plainToken);

            var refreshToken = new RefreshToken(
                user.Id,
                deviceId,
                tokenHash,
                utcNow,
                utcNow.Add(authSettings.RefreshTokenLifetime),
                command.IpAddress,
                command.UserAgent);

            try
            {
                await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
                return Result<(RefreshToken Entity, string PlainToken)>.Success((refreshToken, plainToken));
            }
            catch (UniqueConstraintViolationException ex)
                when (ex.ConstraintCode == PersistenceConstraintCodes.RefreshToken.UserDeviceActiveUnique)
            {
                logger.LogInformation(
                    "Login detected concurrent active device session due to uniqueness constraint. UserId={UserId}, DeviceId={DeviceId}, Constraint={ConstraintName}",
                    user.Id,
                    deviceId,
                    ex.ConstraintName);

                var activeDeviceSession = await refreshTokenRepository.GetActiveByUserAndDeviceAsync(
                    user.Id,
                    deviceId,
                    utcNow,
                    cancellationToken);

                if (activeDeviceSession is not null
                    && TryReuseActiveDeviceSession(user, activeDeviceSession, command, utcNow).IsSuccess)
                {
                    return Result<(RefreshToken Entity, string PlainToken)>.Success((
                        activeDeviceSession,
                        command.CurrentRefreshToken!));
                }

                return Result<(RefreshToken Entity, string PlainToken)>.Failure(AuthError.AlreadyLoggedInOnThisDevice());
            }
            catch (UniqueConstraintViolationException ex)
                when (ex.ConstraintCode == PersistenceConstraintCodes.RefreshToken.TokenHashUnique
                      && attempt < MaxRefreshTokenGenerationAttempts)
            {
                logger.LogWarning(
                    "Refresh token hash collision during login. Attempt={Attempt}, Constraint={ConstraintName}",
                    attempt,
                    ex.ConstraintName);
            }
        }

        logger.LogError("Refresh token creation failed after maximum retries for UserId={UserId}", user.Id);
        return Result<(RefreshToken Entity, string PlainToken)>.Failure(AuthError.TokenGenerationFailed());
    }

    private async Task<Result<User>> ValidateAndAuthenticateAsync(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var emailResult = NormalizeAndValidateEmail(command.Email);
        if (emailResult.IsFailure)
        {
            return Result<User>.Failure(emailResult.Error);
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return Result<User>.Failure(AuthError.InvalidPassword());
        }

        var normalizedEmail = emailResult.Value!;
        var user = await userAuthRepository.GetByEmailForAuthAsync(normalizedEmail, cancellationToken);
        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            logger.LogInformation("Login failed due to invalid credentials. Email={Email}", normalizedEmail);
            return Result<User>.Failure(AuthError.InvalidCredentials());
        }

        if (user.AuthInfo is null)
        {
            logger.LogError("Login failed because AuthInfo is missing for UserId={UserId}", user.Id);
            return Result<User>.Failure(AuthError.InvalidCredentials());
        }

        return Result<User>.Success(user);
    }

    private Result<AuthTokensResult> TryReuseActiveDeviceSession(
        User user,
        RefreshToken activeDeviceSession,
        LoginCommand command,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(command.CurrentRefreshToken))
        {
            logger.LogInformation(
                "Login rejected because active session exists but request did not include refresh cookie. UserId={UserId}, DeviceId={DeviceId}",
                user.Id,
                command.DeviceId);

            return Result<AuthTokensResult>.Failure(AuthError.AlreadyLoggedInOnThisDevice());
        }

        var currentRefreshTokenHash = refreshTokenService.HashToken(command.CurrentRefreshToken);
        if (!string.Equals(currentRefreshTokenHash, activeDeviceSession.TokenHash, StringComparison.Ordinal))
        {
            logger.LogInformation(
                "Login rejected because refresh cookie does not match active device session. UserId={UserId}, DeviceId={DeviceId}",
                user.Id,
                command.DeviceId);

            return Result<AuthTokensResult>.Failure(AuthError.AlreadyLoggedInOnThisDevice());
        }

        var accessToken = accessTokenService.Create(user.Id, user.Email, user.AuthInfo!.AuthVersion, utcNow);

        logger.LogInformation(
            "Login treated as idempotent for active device session. UserId={UserId}, DeviceId={DeviceId}",
            user.Id,
            command.DeviceId);

        return Result<AuthTokensResult>.Success(new AuthTokensResult(
            accessToken,
            command.CurrentRefreshToken,
            activeDeviceSession.ExpiresAt));
    }

    private static Result<string> NormalizeAndValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<string>.Failure(AuthError.InvalidEmail());
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (!normalizedEmail.Contains('@'))
        {
            return Result<string>.Failure(AuthError.InvalidEmail());
        }

        return Result<string>.Success(normalizedEmail);
    }
}
