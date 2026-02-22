using Listed.Domain.Entities;

namespace Listed.Application.Contracts.Persistence;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<RefreshToken?> GetActiveByUserAndDeviceAsync(Guid userId, Guid deviceId, DateTime utcNow, CancellationToken cancellationToken);
    Task<bool> RotateAsync(Guid currentTokenId, RefreshToken replacementToken, DateTime revokedAtUtc, CancellationToken cancellationToken);
    Task<bool> RevokeAsync(Guid refreshTokenId, DateTime revokedAtUtc, CancellationToken cancellationToken);
    Task<int> RevokeExpiredByUserAndDeviceAsync(Guid userId, Guid deviceId, DateTime revokedAtUtc, CancellationToken cancellationToken);
    Task<int> RevokeAllByUserIdAsync(Guid userId, DateTime revokedAtUtc, CancellationToken cancellationToken);
}
