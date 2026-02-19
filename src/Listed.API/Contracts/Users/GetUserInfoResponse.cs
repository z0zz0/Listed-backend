namespace Listed.API.Contracts.Users;

public sealed record GetUserInfoResponse(
    string Nationality,
    string FirstName,
    string LastName,
    string PhoneNumber,
    bool HasPhonePrefix,
    string? Biography);
