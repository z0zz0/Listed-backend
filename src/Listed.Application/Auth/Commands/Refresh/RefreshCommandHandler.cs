using Listed.Application.Auth.Errors;
using Listed.Application.Auth.Results;
using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Contracts.Persistence;
using Listed.Application.Contracts.Security;
using Listed.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Listed.Application.Auth.Commands.Refresh;

public sealed class RefreshCommandHandler(
    IUserAuthRepository userAuthRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAccessTokenService accessTokenService,
    IRefreshTokenService refreshTokenService,
    IAuthSettings authSettings,
    IAuthStateStore authStateStore,
    ILogger<RefreshCommandHandler> logger) : ICommandHandler<RefreshCommand, Result<AuthTokensResult>>
{
    private const int MaxRefreshTokenGenerationAttempts = 3;

    public async Task<Result<AuthTokensResult>> Handle(RefreshCommand command, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var contextResult = await ValidateAndLoadRefreshContextAsync(command, utcNow, cancellationToken);
        if (contextResult.IsFailure)
        {
            return Result<AuthTokensResult>.Failure(contextResult.Error);
        }

        var (user, currentToken) = contextResult.Value!;

        var accessToken = accessTokenService.Create(user.Id, user.Email, user.AuthInfo.AuthVersion, utcNow);
        (RefreshToken Entity, string PlainToken)? replacement = null;
        var rotated = false;

        for (var attempt = 1; attempt <= MaxRefreshTokenGenerationAttempts; attempt++)
        {
            replacement = CreateReplacementRefreshToken(user, currentToken.DeviceId, utcNow, command);

            try
            {
                rotated = await refreshTokenRepository.RotateAsync(
                    currentToken.Id,
                    replacement.Value.Entity,
                    utcNow,
                    cancellationToken);
            }
            catch (UniqueConstraintViolationException ex)
                when (ex.ConstraintCode == PersistenceConstraintCodes.RefreshToken.TokenHashUnique
                      && attempt < MaxRefreshTokenGenerationAttempts)
            {
                logger.LogWarning(
                    "Refresh token hash collision during rotate. Attempt={Attempt}, Constraint={ConstraintName}",
                    attempt,
                    ex.ConstraintName);
                continue;
            }

            break;
        }

        if (!rotated || replacement is null)
        {
            await RevokeAllSessionsAsync(user.Id, utcNow, cancellationToken);
            return Result<AuthTokensResult>.Failure(AuthError.ReusedRefreshToken());
        }

        await authStateStore.SetUserAuthVersionAsync(user.Id, user.AuthInfo.AuthVersion, cancellationToken);

        logger.LogInformation("Refresh succeeded. UserId={UserId}", user.Id);

        return Result<AuthTokensResult>.Success(new AuthTokensResult(
            accessToken,
            replacement.Value.PlainToken,
            replacement.Value.Entity.ExpiresAt));
    }

    private async Task<Result<(User User, RefreshToken CurrentToken)>> ValidateAndLoadRefreshContextAsync(
        RefreshCommand command,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return Result<(User User, RefreshToken CurrentToken)>.Failure(AuthError.MissingRefreshToken());
        }

        if (!command.DeviceId.HasValue || command.DeviceId.Value == Guid.Empty)
        {
            logger.LogWarning("Refresh failed because device id cookie is missing or invalid.");
            return Result<(User User, RefreshToken CurrentToken)>.Failure(AuthError.InvalidRefreshToken());
        }

        var tokenHash = refreshTokenService.HashToken(command.RefreshToken);
        var currentToken = await refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);
        if (currentToken is null)
        {
            return Result<(User User, RefreshToken CurrentToken)>.Failure(AuthError.InvalidRefreshToken());
        }

        if (currentToken.IsRevoked)
        {
            await RevokeAllSessionsAsync(currentToken.UserId, utcNow, cancellationToken);
            return Result<(User User, RefreshToken CurrentToken)>.Failure(AuthError.ReusedRefreshToken());
        }

        if (currentToken.IsExpired(utcNow))
        {
            await refreshTokenRepository.RevokeAsync(currentToken.Id, utcNow, cancellationToken);
            return Result<(User User, RefreshToken CurrentToken)>.Failure(AuthError.ExpiredRefreshToken());
        }

        if (currentToken.DeviceId != command.DeviceId.Value)
        {
            logger.LogWarning(
                "Refresh failed because device id did not match token session. UserId={UserId}, TokenId={TokenId}",
                currentToken.UserId,
                currentToken.Id);

            return Result<(User User, RefreshToken CurrentToken)>.Failure(AuthError.InvalidRefreshToken());
        }

        var user = await userAuthRepository.GetByIdForAuthAsync(currentToken.UserId, cancellationToken);
        if (user is null || user.AuthInfo is null)
        {
            return Result<(User User, RefreshToken CurrentToken)>.Failure(AuthError.UserNotFound(currentToken.UserId));
        }

        return Result<(User User, RefreshToken CurrentToken)>.Success((user, currentToken));
    }

    private async Task RevokeAllSessionsAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        await refreshTokenRepository.RevokeAllByUserIdAsync(userId, utcNow, cancellationToken);
        var incremented = await userAuthRepository.IncrementAuthVersionAsync(userId, cancellationToken);
        if (!incremented)
        {
            return;
        }

        var user = await userAuthRepository.GetByIdForAuthAsync(userId, cancellationToken);
        if (user is null || user.AuthInfo is null)
        {
            return;
        }

        await authStateStore.SetUserAuthVersionAsync(userId, user.AuthInfo.AuthVersion, cancellationToken);
    }

    private (RefreshToken Entity, string PlainToken) CreateReplacementRefreshToken(
        User user,
        Guid deviceId,
        DateTime utcNow,
        RefreshCommand command)
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

        return (refreshToken, plainToken);
    }
}
