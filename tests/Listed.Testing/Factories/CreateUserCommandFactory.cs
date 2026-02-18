using Listed.Application.Users.Commands.CreateUser;

namespace Listed.Testing.Factories;

public static class CreateUserCommandFactory
{
    public static CreateUserCommand Valid(string? email = null, string? password = null)
    {
        return new CreateUserCommand(
            email ?? "new@test.io",
            password ?? "StrongPass123!");
    }

    public static CreateUserCommand InvalidEmail()
    {
        return Valid(email: "not-an-email");
    }

    public static CreateUserCommand ShortPassword()
    {
        return Valid(password: "1234567");
    }
}
