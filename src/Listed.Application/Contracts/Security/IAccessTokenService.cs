namespace Listed.Application.Contracts.Security;

public interface IAccessTokenService
{
    AccessTokenResult Create(Guid userId, string email, int authVersion, DateTime utcNow);
}
