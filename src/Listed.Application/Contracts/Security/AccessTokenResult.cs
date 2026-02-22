namespace Listed.Application.Contracts.Security;

public sealed record AccessTokenResult(
    string Token,
    DateTime ExpiresAtUtc,
    int ExpiresInSeconds);
