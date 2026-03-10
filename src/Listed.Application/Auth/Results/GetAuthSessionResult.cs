namespace Listed.Application.Auth.Results;

public sealed record GetAuthSessionResult(Guid UserId, string Email, int AuthVersion);
