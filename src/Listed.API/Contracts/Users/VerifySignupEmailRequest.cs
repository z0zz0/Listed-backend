namespace Listed.API.Contracts.Users;

public sealed record VerifySignupEmailRequest(Guid SignupId, string VerificationCode);
