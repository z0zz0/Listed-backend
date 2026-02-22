namespace Listed.Application.Contracts.Security;

public interface IAuthStateStore
{
    Task<bool> IsAccessTokenRevokedAsync(string tokenId, CancellationToken cancellationToken);
    Task RevokeAccessTokenAsync(string tokenId, TimeSpan ttl, CancellationToken cancellationToken);
    Task<int?> GetUserAuthVersionAsync(Guid userId, CancellationToken cancellationToken);
    Task SetUserAuthVersionAsync(Guid userId, int authVersion, CancellationToken cancellationToken);
}
