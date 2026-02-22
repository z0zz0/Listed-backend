using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Listed.Application.Contracts.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Listed.Infrastructure.Security;

public sealed class AccessTokenService(IOptions<AuthOptions> authOptions) : IAccessTokenService
{
    private const string AuthVersionClaim = "auth_version";
    private readonly AuthOptions _authOptions = authOptions.Value;

    public AccessTokenResult Create(Guid userId, string email, int authVersion, DateTime utcNow)
    {
        var expiresAtUtc = utcNow.Add(_authOptions.AccessTokenLifetime);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(AuthVersionClaim, authVersion.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authOptions.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwtToken = new JwtSecurityToken(
            issuer: _authOptions.Issuer,
            audience: _authOptions.Audience,
            claims: claims,
            notBefore: utcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var token = new JwtSecurityTokenHandler().WriteToken(jwtToken);
        return new AccessTokenResult(token, expiresAtUtc, (int)_authOptions.AccessTokenLifetime.TotalSeconds);
    }
}
