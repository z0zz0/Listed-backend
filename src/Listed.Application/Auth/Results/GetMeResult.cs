namespace Listed.Application.Auth.Results;

public sealed record GetMeResult(Guid UserId, string Email, int AuthVersion);
