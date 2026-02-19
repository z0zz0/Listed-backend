using Listed.Application.Users.Queries.GetUserByEmail;

namespace Listed.Testing.Factories;

public static class GetUserByEmailQueryFactory
{
    public static GetUserByEmailQuery Valid(string? email = null)
    {
        return new GetUserByEmailQuery(email ?? "user@test.io");
    }

    public static GetUserByEmailQuery InvalidEmail()
    {
        return Valid(email: "not-an-email");
    }

    public static GetUserByEmailQuery EmptyEmail()
    {
        return Valid(email: string.Empty);
    }
}
