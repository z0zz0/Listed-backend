namespace Listed.API.Contracts.Users;

public sealed record VerifySignupEmailResponse(Guid SignupId, DateTime VerifiedAtUtc);
