namespace Listed.Application.Contracts.Security;

public interface IPasswordHasher
{
    string AlgorithmName { get; }
    string Hash(string plainTextPassword);
    bool Verify(string plainTextPassword, string passwordHash);
}
