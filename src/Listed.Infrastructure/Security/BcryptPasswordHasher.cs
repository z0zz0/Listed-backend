using Listed.Application.Contracts.Security;

namespace Listed.Infrastructure.Security;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string AlgorithmName => "bcrypt";

    public string Hash(string plainTextPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainTextPassword);
    }

    public bool Verify(string plainTextPassword, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(plainTextPassword, passwordHash);
    }
}
