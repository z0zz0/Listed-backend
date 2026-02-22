using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Contracts.Persistence;
using Listed.Application.Contracts.Security;
using Microsoft.Extensions.Logging;

namespace Listed.Application.Auth.Commands.Logout;

public sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenService refreshTokenService,
    IAuthStateStore authStateStore,
    ILogger<LogoutCommandHandler> logger) : ICommandHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        await TryRevokeCurrentRefreshTokenAsync(command, utcNow, cancellationToken);

        await RevokeCurrentAccessTokenAsync(command, utcNow, cancellationToken);

        logger.LogInformation("Logout succeeded for UserId={UserId}", command.UserId);

        return Result.Success();
    }

    private async Task TryRevokeCurrentRefreshTokenAsync(
        LogoutCommand command,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return;
        }

        var tokenHash = refreshTokenService.HashToken(command.RefreshToken);
        var refreshToken = await refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        if (refreshToken is null || refreshToken.UserId != command.UserId || refreshToken.IsRevoked)
        {
            return;
        }

        await refreshTokenRepository.RevokeAsync(refreshToken.Id, utcNow, cancellationToken);
    }

    private async Task RevokeCurrentAccessTokenAsync(
        LogoutCommand command,
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
