namespace Listed.Application.Contracts.Security;

public interface IAccessTokenService
{
    AccessTokenResult Create(Guid userId, Guid sessionId, string email, int authVersion, DateTime utcNow);
}
