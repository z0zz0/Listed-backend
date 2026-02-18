using Listed.API.Contracts.Users;

namespace Listed.Testing.Factories;

public static class CreateUserRequestFactory
{
    public static CreateUserRequest Valid(string? email = null, string? password = null)
    {
        return new CreateUserRequest(
            email ?? CreateEmail("ok"),
            password ?? "StrongPass123!");
    }

    public static CreateUserRequest InvalidEmail()
    {
        return Valid(email: "invalid-email");
    }

    public static CreateUserRequest ShortPassword()
    {
        return Valid(email: CreateEmail("short"), password: "1234567");
    }

    public static string CreateEmail(string prefix)
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        return $"{prefix}.{token}@t.io";
    }
}
