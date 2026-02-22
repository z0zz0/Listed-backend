using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Listed.API.Contracts.Auth;
using Listed.Application.Auth.Results;
using Listed.Application.Contracts.Security;
using Microsoft.AspNetCore.Http;

namespace Listed.API.Common.Utils;

public static class AuthUtils
{
    private const string DeviceIdCookieName = "listed_device_id";

    public static AccessTokenResponse MapToAccessTokenResponse(this AccessTokenResult accessTokenResult)
    {
        return new AccessTokenResponse(
            accessTokenResult.Token,
            accessTokenResult.ExpiresAtUtc,
            accessTokenResult.ExpiresInSeconds);
    }

    public static GetMeResponse MapToGetMeResponse(this GetMeResult meResult)
    {
        return new GetMeResponse(meResult.UserId, meResult.Email, meResult.AuthVersion);
    }

    public static Guid? TryGetUserId(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(subject, out var userId) ? userId : null;
    }

    public static string? TryGetAccessTokenId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
    }

    public static DateTime? TryGetAccessTokenExpiresAtUtc(this ClaimsPrincipal principal)
    {
        var expiresClaim = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        if (!long.TryParse(expiresClaim, out var epochSeconds))
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeSeconds(epochSeconds).UtcDateTime;
    }

    public static Guid GetOrCreateDeviceId(HttpRequest request, HttpResponse response)
    {
        if (TryGetDeviceId(request, out var existingDeviceId))
        {
            return existingDeviceId;
        }

        var deviceId = Guid.NewGuid();
        WriteDeviceIdCookie(request, response, deviceId);

        return deviceId;
    }

    public static bool TryGetDeviceId(HttpRequest request, out Guid deviceId)
    {
        if (request.Cookies.TryGetValue(DeviceIdCookieName, out var existingValue)
            && Guid.TryParse(existingValue, out var existingDeviceId)
            && existingDeviceId != Guid.Empty)
        {
            deviceId = existingDeviceId;
            return true;
        }

        deviceId = Guid.Empty;
        return false;
    }

    public static void WriteRefreshTokenCookie(
        HttpRequest request,
        HttpResponse response,
        string refreshTokenCookieName,
        string refreshToken,
        DateTime refreshTokenExpiresAtUtc)
    {
        response.Cookies.Append(
            refreshTokenCookieName,
            refreshToken,
            BuildRefreshTokenCookieOptions(request, refreshTokenExpiresAtUtc));
    }

    public static void DeleteRefreshTokenCookie(
        HttpRequest request,
        HttpResponse response,
        string refreshTokenCookieName)
    {
        response.Cookies.Delete(
            refreshTokenCookieName,
            BuildRefreshTokenCookieOptions(request, null));
    }

    private static void WriteDeviceIdCookie(HttpRequest request, HttpResponse response, Guid deviceId)
    {
        response.Cookies.Append(
            DeviceIdCookieName,
            deviceId.ToString("D"),
            BuildDeviceIdCookieOptions(request, DateTime.UtcNow.AddYears(2)));
    }

    private static CookieOptions BuildRefreshTokenCookieOptions(HttpRequest request, DateTime? expiresAtUtc)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = ShouldUseSecureCookies(request),
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
            Expires = expiresAtUtc
        };
    }

    private static CookieOptions BuildDeviceIdCookieOptions(HttpRequest request, DateTime? expiresAtUtc)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = ShouldUseSecureCookies(request),
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = expiresAtUtc
        };
    }

    private static bool ShouldUseSecureCookies(HttpRequest request)
    {
        return request.IsHttps;
    }
}
