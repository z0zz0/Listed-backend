using Listed.Application.Contracts.Security;

namespace Listed.Application.Auth.Results;

public sealed record AuthTokensResult(
    AccessTokenResult AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
