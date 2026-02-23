using System.IdentityModel.Tokens.Jwt;
using Listed.Application.Contracts.Persistence;
using Listed.Application.Contracts.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;

namespace Listed.Infrastructure.Security;

public sealed class JwtTokenValidationService(
    IAuthStateStore authStateStore,
    IUserAuthRepository userAuthRepository,
    ILogger<JwtTokenValidationService> logger)
{
    private const string AuthVersionClaim = "auth_version";
    private const string SessionIdClaim = "sid";

    public async Task ValidateAsync(TokenValidatedContext context)
    {
        try
        {
            var principal = context.Principal;
            if (principal is null)
            {
                context.Fail("Missing principal.");
                return;
            }

            var tokenId = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var sessionIdClaim = principal.FindFirst(SessionIdClaim)?.Value;
            var tokenAuthVersionClaim = principal.FindFirst(AuthVersionClaim)?.Value;

            if (string.IsNullOrWhiteSpace(tokenId)
                || string.IsNullOrWhiteSpace(subject)
                || string.IsNullOrWhiteSpace(tokenAuthVersionClaim))
            {
                context.Fail("Required token claims are missing.");
                return;
            }

            if (!Guid.TryParse(subject, out var userId))
            {
                context.Fail("Invalid token subject.");
                return;
            }

            Guid? sessionId = null;
            if (!string.IsNullOrWhiteSpace(sessionIdClaim))
            {
                if (!Guid.TryParse(sessionIdClaim, out var parsedSessionId))
                {
                    context.Fail("Invalid session id claim.");
                    return;
                }

                sessionId = parsedSessionId;
            }

            if (!int.TryParse(tokenAuthVersionClaim, out var tokenAuthVersion))
            {
                context.Fail("Invalid auth version claim.");
                return;
            }

            var cancellationToken = context.HttpContext.RequestAborted;
            var isRevoked = await authStateStore.IsAccessTokenRevokedAsync(tokenId, cancellationToken);
            if (isRevoked)
            {
                context.Fail("Access token has been revoked.");
                return;
            }

            if (sessionId.HasValue)
            {
                var isSessionRevoked = await authStateStore.IsSessionRevokedAsync(sessionId.Value, cancellationToken);
                if (isSessionRevoked)
                {
                    context.Fail("Session has been revoked.");
                    return;
                }
            }

            var currentAuthVersion = await authStateStore.GetUserAuthVersionAsync(userId, cancellationToken);
            if (!currentAuthVersion.HasValue)
            {
                var user = await userAuthRepository.GetByIdForAuthAsync(userId, cancellationToken);
                if (user is null || user.AuthInfo is null)
                {
                    context.Fail("User does not exist.");
                    return;
                }

                currentAuthVersion = user.AuthInfo.AuthVersion;
                await authStateStore.SetUserAuthVersionAsync(userId, user.AuthInfo.AuthVersion, cancellationToken);
            }

            if (tokenAuthVersion != currentAuthVersion.Value)
            {
                context.Fail("Token auth version is stale.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "JWT token validation failed due to auth-state dependency failure.");
            context.Fail("Token validation is unavailable.");
        }
    }
}
