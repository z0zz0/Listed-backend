using Listed.Application.Contracts.Security;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Listed.Infrastructure.Security;

public sealed class ValkeyAuthStateStore(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<ValkeyAuthStateStore> logger) : IAuthStateStore
{
    private static readonly TimeSpan AuthVersionCacheTtl = TimeSpan.FromDays(90);
    private const string RevokedTokenKeyPrefix = "auth:revoked:jti:";
    private const string UserAuthVersionKeyPrefix = "auth:user:version:";
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task<bool> IsAccessTokenRevokedAsync(string tokenId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = $"{RevokedTokenKeyPrefix}{tokenId}";

        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Valkey failed while checking revoked token. TokenId={TokenId}", tokenId);
            throw;
        }
    }

    public async Task RevokeAccessTokenAsync(string tokenId, TimeSpan ttl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        var key = $"{RevokedTokenKeyPrefix}{tokenId}";

        try
        {
            await _database.StringSetAsync(key, "1", ttl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Valkey failed while revoking access token. TokenId={TokenId}", tokenId);
            throw;
        }
    }

    public async Task<int?> GetUserAuthVersionAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = $"{UserAuthVersionKeyPrefix}{userId:N}";

        try
        {
            var value = await _database.StringGetAsync(key);
            if (value.IsNullOrEmpty)
            {
                return null;
            }

            return int.TryParse(value.ToString(), out var authVersion)
                ? authVersion
                : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Valkey failed while reading auth version. UserId={UserId}", userId);
            throw;
        }
    }

    public async Task SetUserAuthVersionAsync(Guid userId, int authVersion, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = $"{UserAuthVersionKeyPrefix}{userId:N}";

        try
        {
            await _database.StringSetAsync(key, authVersion, AuthVersionCacheTtl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Valkey failed while setting auth version. UserId={UserId}", userId);
            throw;
        }
    }
}
