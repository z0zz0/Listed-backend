namespace Listed.Application.Contracts.Signup;

public sealed record SignupVerificationState(
    string Email,
    string CodeHash,
    DateTime ExpiresAtUtc,
    int FailedAttempts,
    bool IsVerified,
    DateTime? VerifiedAtUtc,
    string? FirstName = null,
    string? LastName = null,
    DateOnly? DateOfBirth = null);
