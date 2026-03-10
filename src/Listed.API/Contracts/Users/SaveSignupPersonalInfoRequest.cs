namespace Listed.API.Contracts.Users;

public sealed record SaveSignupPersonalInfoRequest(
    Guid SignupId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth);
