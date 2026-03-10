namespace Listed.Application.Users.Results;

public sealed record StartSignupResult(Guid SignupId, string Email, DateTime CodeExpiresAtUtc);
