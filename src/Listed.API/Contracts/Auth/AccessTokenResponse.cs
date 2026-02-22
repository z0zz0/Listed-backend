namespace Listed.API.Contracts.Auth;

public sealed record AccessTokenResponse(string Token, DateTime ExpiresAtUtc, int ExpiresInSeconds);
