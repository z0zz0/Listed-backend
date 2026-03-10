namespace Listed.API.Contracts.Users;

public sealed record StartSignupResponse(Guid SignupId, string Email, DateTime CodeExpiresAtUtc);
