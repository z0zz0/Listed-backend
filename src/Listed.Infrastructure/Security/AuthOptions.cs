using Listed.Application.Contracts.Security;

namespace Listed.Infrastructure.Security;

public sealed class AuthOptions : IAuthSettings
{
    public const string SectionName = "Auth";

    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string SigningKey { get; init; }
    public required int AccessTokenLifetimeMinutes { get; init; }
    public required int RefreshTokenLifetimeDays { get; init; }
    public required string RefreshTokenCookieName { get; init; }

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(AccessTokenLifetimeMinutes);
    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(RefreshTokenLifetimeDays);

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("Auth:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Auth:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(SigningKey))
        {
            throw new InvalidOperationException("Auth:SigningKey is required.");
        }

        if (SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Auth:SigningKey must be at least 32 characters.");
        }

        if (AccessTokenLifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("Auth:AccessTokenLifetimeMinutes must be greater than 0.");
        }

        if (RefreshTokenLifetimeDays <= 0)
        {
            throw new InvalidOperationException("Auth:RefreshTokenLifetimeDays must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(RefreshTokenCookieName))
        {
            throw new InvalidOperationException("Auth:RefreshTokenCookieName is required.");
        }
    }
}
