using Listed.Application.Auth.Errors;
using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Contracts.Persistence;
using Listed.Application.Contracts.Security;
using Listed.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Listed.Application.Auth.Commands.LogoutAll;

public sealed class LogoutAllCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserAuthRepository userAuthRepository,
    IRefreshTokenService refreshTokenService,
    IAuthStateStore authStateStore,
    ILogger<LogoutAllCommandHandler> logger) : ICommandHandler<LogoutAllCommand, Result>
{
    public async Task<Result> Handle(LogoutAllCommand command, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var validationResult = await ValidateCurrentRefreshSessionAsync(command, utcNow, cancellationToken);
        if (validationResult.IsFailure)
        {
            logger.LogInformation(
                "LogoutAll rejected because refresh token did not match current access session. UserId={UserId}",
                command.UserId);
            return Result.Failure(validationResult.Error);
        }

        await refreshTokenRepository.RevokeAllByUserIdAsync(command.UserId, utcNow, cancellationToken);

        var incremented = await userAuthRepository.IncrementAuthVersionAsync(command.UserId, cancellationToken);
        if (!incremented)
        {
            return Result.Failure(AuthError.SessionNotFound(command.UserId));
        }

        var userResult = await LoadUserWithAuthInfoAsync(command.UserId, cancellationToken);
        if (userResult.IsFailure)
        {
            return Result.Failure(userResult.Error);
        }

        var user = userResult.Value!;
        await authStateStore.SetUserAuthVersionAsync(user.Id, user.AuthInfo.AuthVersion, cancellationToken);
        await RevokeCurrentAccessTokenAsync(command, utcNow, cancellationToken);

        logger.LogInformation("LogoutAll succeeded for UserId={UserId}", command.UserId);

        return Result.Success();
    }

    private async Task<Result> ValidateCurrentRefreshSessionAsync(
        LogoutAllCommand command,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken) || !command.AccessTokenSessionId.HasValue)
        {
            return Result.Failure(AuthError.InvalidRefreshToken());
        }

        var tokenHash = refreshTokenService.HashToken(command.RefreshToken);
        var refreshToken = await refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);
        if (refreshToken is null
            || refreshToken.UserId != command.UserId
            || refreshToken.IsRevoked
            || refreshToken.IsExpired(utcNow)
            || refreshToken.SessionId != command.AccessTokenSessionId.Value)
        {
            return Result.Failure(AuthError.InvalidRefreshToken());
        }

        return Result.Success();
    }

    private async Task<Result<User>> LoadUserWithAuthInfoAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userAuthRepository.GetByIdForAuthAsync(userId, cancellationToken);
        if (user is null || user.AuthInfo is null)
        {
            return Result<User>.Failure(AuthError.SessionNotFound(userId));
        }

        return Result<User>.Success(user);
    }

    private async Task RevokeCurrentAccessTokenAsync(
        LogoutAllCommand command,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.AccessTokenId) || command.AccessTokenExpiresAtUtc is null)
        {
            return;
        }

        var ttl = command.AccessTokenExpiresAtUtc.Value - utcNow;
        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        await authStateStore.RevokeAccessTokenAsync(command.AccessTokenId, ttl, cancellationToken);
    }
}
