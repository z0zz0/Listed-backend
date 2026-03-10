namespace Listed.API.Contracts.Auth;

public sealed record GetAuthSessionResponse(Guid UserId, string Email, int AuthVersion);
