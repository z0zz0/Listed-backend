using Listed.Domain.Entities;

namespace Listed.Testing.Factories;

public static class UserFactory
{
    public static User Valid(string? email = null, string? passwordHash = null, string? algorithm = null)
    {
        return new User(
            email ?? "user@test.io",
            passwordHash ?? "hashed-password",
            algorithm ?? "bcrypt");
    }
}
