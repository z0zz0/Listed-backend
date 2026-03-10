namespace Listed.Application.Auth.Results;

public sealed record LogoutResult(bool ShouldDeleteRefreshTokenCookie);
