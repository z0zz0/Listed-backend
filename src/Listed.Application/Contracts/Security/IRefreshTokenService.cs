namespace Listed.Application.Contracts.Security;

public interface IRefreshTokenService
{
    string GenerateToken();
    string HashToken(string token);
}
