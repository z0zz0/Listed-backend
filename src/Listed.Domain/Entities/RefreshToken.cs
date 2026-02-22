namespace Listed.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid DeviceId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? CreatedByUserAgent { get; private set; }

    public AuthInfo AuthInfo { get; private set; } = null!;

    private RefreshToken() { }

    public RefreshToken(
        Guid userId,
        Guid deviceId,
        string tokenHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        string? createdByIp,
        string? createdByUserAgent)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Token hash cannot be empty.", nameof(tokenHash));
        }

        if (deviceId == Guid.Empty)
        {
            throw new ArgumentException("Device id cannot be empty.", nameof(deviceId));
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException("Expiration must be after creation.");
        }

        Id = Guid.NewGuid();
        UserId = userId;
        DeviceId = deviceId;
        TokenHash = tokenHash;
        CreatedAt = createdAtUtc;
        ExpiresAt = expiresAtUtc;
        CreatedByIp = createdByIp;
        CreatedByUserAgent = createdByUserAgent;
    }

    public bool IsExpired(DateTime utcNow) => ExpiresAt <= utcNow;
    public bool IsRevoked => RevokedAt.HasValue;
}
