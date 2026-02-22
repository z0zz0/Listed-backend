namespace Listed.API.Contracts.Auth;

public sealed record GetMeResponse(Guid UserId, string Email, int AuthVersion);
