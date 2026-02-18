using Listed.Domain.Entities;

namespace Listed.Testing.Factories;

public static class UserInfoFactory
{
    public static UserInfo Valid(
        string? nationality = null,
        string? nationalIdNumber = null,
        string? firstName = null,
        string? lastName = null,
        string? phoneNumber = null,
        bool hasPhonePrefix = true,
        string? biography = null)
    {
        return new UserInfo(
            nationality ?? "SE",
            nationalIdNumber ?? "NIN-123",
            firstName ?? "John",
            lastName ?? "Doe",
            phoneNumber ?? "0700000000",
            hasPhonePrefix,
            biography);
    }
}
