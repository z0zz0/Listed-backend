using Listed.Domain.Entities;

namespace Listed.Testing.Factories;

public static class UserInfoFactory
{
    public static UserInfo Valid(
        Guid? userId = null,
        string? firstName = null,
        string? lastName = null,
        DateOnly? dateOfBirth = null,
        string? nationality = null,
        string? nationalIdNumber = null,
        string? phoneNumber = null,
        bool? hasPhonePrefix = true,
        string? biography = null)
    {
        return new UserInfo(
            userId ?? Guid.NewGuid(),
            firstName ?? "John",
            lastName ?? "Doe",
            dateOfBirth ?? new DateOnly(1990, 1, 1),
            nationality ?? "SE",
            nationalIdNumber ?? "NIN-123",
            phoneNumber ?? "0700000000",
            hasPhonePrefix,
            biography);
    }
}
