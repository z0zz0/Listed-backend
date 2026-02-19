namespace Listed.Application.Users.Results;

public sealed record GetUserInfoResult(
    string Nationality,
    string FirstName,
    string LastName,
    string PhoneNumber,
    bool HasPhonePrefix,
    string? Biography);
