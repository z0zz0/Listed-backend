namespace Listed.Application.Users.Results;

public sealed record VerifySignupEmailResult(Guid SignupId, DateTime VerifiedAtUtc);
