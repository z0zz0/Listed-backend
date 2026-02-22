using Listed.API.Contracts.Auth;

namespace Listed.Testing.Factories;

public static class LoginRequestFactory
{
    public static LoginRequest Valid(string? email = null, string? password = null)
    {
        return new LoginRequest(
            email ?? CreateEmail(),
            password ?? "StrongPass123!");
    }

    public static LoginRequest InvalidEmail(string? password = null)
    {
        return new LoginRequest("invalid-email", password ?? "StrongPass123!");
    }

    public static LoginRequest InvalidPassword(string? email = null)
    {
        return new LoginRequest(email ?? CreateEmail(), "");
    }

    public static string CreateEmail(string prefix = "login")
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        return $"{prefix}.{token}@t.io";
    }
}
