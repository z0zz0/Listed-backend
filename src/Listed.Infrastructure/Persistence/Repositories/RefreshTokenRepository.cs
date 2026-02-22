using Listed.Application.Contracts.Persistence;
using Listed.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Listed.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(ListedDbContext dbContext) : IRefreshTokenRepository
{
    private static readonly IReadOnlyDictionary<string, string> UniqueConstraintCodeByName =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PersistenceConstraintNames.RefreshToken.TokenHashUnique] = PersistenceConstraintCodes.RefreshToken.TokenHashUnique,
            [PersistenceConstraintNames.RefreshToken.UserDeviceActiveUnique] = PersistenceConstraintCodes.RefreshToken.UserDeviceActiveUnique
        };

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await SaveChangesWithUniqueConstraintMapping(cancellationToken);
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);
    }

    public Task<RefreshToken?> GetActiveByUserAndDeviceAsync(
        Guid userId,
        Guid deviceId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(rt =>
                rt.UserId == userId
                && rt.DeviceId == deviceId
                && rt.RevokedAt == null
                && rt.ExpiresAt > utcNow,
                cancellationToken);
    }

    public async Task<bool> RotateAsync(
        Guid currentTokenId,
        RefreshToken replacementToken,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var affectedRows = await dbContext.RefreshTokens
            .Where(rt => rt.Id == currentTokenId && rt.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.RevokedAt, revokedAtUtc)
                .SetProperty(rt => rt.ReplacedByTokenId, replacementToken.Id), cancellationToken);

        if (affectedRows == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await dbContext.RefreshTokens.AddAsync(replacementToken, cancellationToken);
        await SaveChangesWithUniqueConstraintMapping(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<bool> RevokeAsync(Guid refreshTokenId, DateTime revokedAtUtc, CancellationToken cancellationToken)
    {
        var affectedRows = await dbContext.RefreshTokens
            .Where(rt => rt.Id == refreshTokenId && rt.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.RevokedAt, revokedAtUtc), cancellationToken);

        return affectedRows > 0;
    }

    public async Task<int> RevokeAllByUserIdAsync(Guid userId, DateTime revokedAtUtc, CancellationToken cancellationToken)
    {
        var affectedRows = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.RevokedAt, revokedAtUtc), cancellationToken);

        return affectedRows;
    }

    public async Task<int> RevokeExpiredByUserAndDeviceAsync(
        Guid userId,
        Guid deviceId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken)
    {
        var affectedRows = await dbContext.RefreshTokens
            .Where(rt =>
                rt.UserId == userId
                && rt.DeviceId == deviceId
                && rt.RevokedAt == null
                && rt.ExpiresAt <= revokedAtUtc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.RevokedAt, revokedAtUtc), cancellationToken);

        return affectedRows;
    }

    private async Task SaveChangesWithUniqueConstraintMapping(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgresEx
                                           && postgresEx.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            var constraintName = postgresEx.ConstraintName;
            var constraintCode = constraintName is not null && UniqueConstraintCodeByName.TryGetValue(constraintName, out var code)
                ? code
                : PersistenceConstraintCodes.Common.UnknownUnique;

            throw new UniqueConstraintViolationException(constraintCode, constraintName);
        }
    }
}
