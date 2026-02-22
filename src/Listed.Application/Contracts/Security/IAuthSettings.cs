namespace Listed.Application.Contracts.Security;

public interface IAuthSettings
{
    TimeSpan AccessTokenLifetime { get; }
    TimeSpan RefreshTokenLifetime { get; }
    string RefreshTokenCookieName { get; }
}
