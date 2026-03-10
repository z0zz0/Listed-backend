using Listed.Application.Common;
using Listed.Application.Auth.Errors;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Contracts.Persistence;
using Listed.Application.Contracts.Security;
using Listed.Application.Auth.Results;
using Microsoft.Extensions.Logging;

namespace Listed.Application.Auth.Commands.Logout;

public sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenService refreshTokenService,
    IAuthStateStore authStateStore,
    ILogger<LogoutCommandHandler> logger) : ICommandHandler<LogoutCommand, Result<LogoutResult>>
{
    public async Task<Result<LogoutResult>> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var refreshTokenResult = await TryRevokeCurrentRefreshTokenAsync(command, utcNow, cancellationToken);
        if (refreshTokenResult.IsFailure)
        {
            logger.LogInformation(
                "Logout rejected because refresh token did not match current access session. UserId={UserId}",
                command.UserId);
            return Result<LogoutResult>.Failure(refreshTokenResult.Error);
        }

        await RevokeCurrentSessionAccessAsync(command, utcNow, cancellationToken);

        logger.LogInformation("Logout succeeded for UserId={UserId}", command.UserId);
        return Result<LogoutResult>.Success(new LogoutResult(true));
    }

    private async Task<Result> TryRevokeCurrentRefreshTokenAsync(
        LogoutCommand command,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return Result.Failure(AuthError.InvalidRefreshToken());
        }

        var tokenHash = refreshTokenService.HashToken(command.RefreshToken);
        var refreshToken = await refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            return Result.Failure(AuthError.InvalidRefreshToken());
        }

        if (refreshToken.UserId != command.UserId || refreshToken.IsRevoked)
        {
            return Result.Failure(AuthError.InvalidRefreshToken());
        }

        if (!command.AccessTokenSessionId.HasValue
            || refreshToken.SessionId != command.AccessTokenSessionId.Value)
        {
            return Result.Failure(AuthError.InvalidRefreshToken());
        }

        var revoked = await refreshTokenRepository.RevokeAsync(refreshToken.Id, utcNow, cancellationToken);
        if (!revoked)
        {
            return Result.Failure(AuthError.InvalidRefreshToken());
        }

        return Result.Success();
    }

    private async Task RevokeCurrentSessionAccessAsync(
        LogoutCommand command,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (command.AccessTokenExpiresAtUtc is null)
        {
            return;
        }

        var ttl = command.AccessTokenExpiresAtUtc.Value - utcNow;
        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        if (command.AccessTokenSessionId.HasValue)
        {
            await authStateStore.RevokeSessionAsync(command.AccessTokenSessionId.Value, ttl, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(command.AccessTokenId))
        {
            await authStateStore.RevokeAccessTokenAsync(command.AccessTokenId, ttl, cancellationToken);
        }
    }
}
