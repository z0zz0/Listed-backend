namespace Listed.API.Contracts.Users;

public sealed record CompleteSignupRequest(Guid SignupId, string Password);
