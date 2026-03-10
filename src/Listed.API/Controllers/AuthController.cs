using Listed.API.Common.ErrorMapping;
using Listed.API.Common.Utils;
using Listed.API.Contracts.Auth;
using Listed.Application.Auth.Commands.Login;
using Listed.Application.Auth.Commands.Logout;
using Listed.Application.Auth.Commands.LogoutAll;
using Listed.Application.Auth.Commands.Refresh;
using Listed.Application.Auth.Queries.GetAuthSession;
using Listed.Application.Auth.Results;
using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Listed.API.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public sealed class AuthController(
    ICommandHandler<LoginCommand, Result<AuthTokensResult>> loginCommandHandler,
    ICommandHandler<RefreshCommand, Result<AuthTokensResult>> refreshCommandHandler,
    ICommandHandler<LogoutCommand, Result<LogoutResult>> logoutCommandHandler,
    ICommandHandler<LogoutAllCommand, Result> logoutAllCommandHandler,
    IQueryHandler<GetAuthSessionQuery, Result<GetAuthSessionResult>> getAuthSessionQueryHandler,
    ResultHttpMapper resultHttpMapper,
    AuthOptions authOptions) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var deviceId = AuthUtils.GetOrCreateDeviceId(Request, Response);
        Request.Cookies.TryGetValue(authOptions.RefreshTokenCookieName, out var currentRefreshToken);

        var command = new LoginCommand(
            request.Email,
            request.Password,
            deviceId,
            currentRefreshToken,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var result = await loginCommandHandler.Handle(command, cancellationToken);

        return result.Match(
            tokens =>
            {
                Response.Cookies.Append(
                    authOptions.RefreshTokenCookieName,
                    tokens.RefreshToken,
                    AuthUtils.BuildRefreshTokenCookieOptions(Request, tokens.RefreshTokenExpiresAtUtc));

                return Ok(tokens.AccessToken.MapToAccessTokenResponse());
            },
            error => resultHttpMapper.ToFailureActionResult(this, error));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(authOptions.RefreshTokenCookieName, out var refreshToken);
        var hasDeviceId = AuthUtils.TryGetDeviceId(Request, out var deviceId);

        var command = new RefreshCommand(
            refreshToken,
            hasDeviceId ? deviceId : null,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var result = await refreshCommandHandler.Handle(command, cancellationToken);

        return result.Match(
            tokens =>
            {
                Response.Cookies.Append(
                    authOptions.RefreshTokenCookieName,
                    tokens.RefreshToken,
                    AuthUtils.BuildRefreshTokenCookieOptions(Request, tokens.RefreshTokenExpiresAtUtc));

                return Ok(tokens.AccessToken.MapToAccessTokenResponse());
            },
            error => resultHttpMapper.ToFailureActionResult(this, error));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = User.TryGetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        Request.Cookies.TryGetValue(authOptions.RefreshTokenCookieName, out var refreshToken);

        var command = new LogoutCommand(
            userId.Value,
            refreshToken,
            User.TryGetAccessTokenSessionId(),
            User.TryGetAccessTokenId(),
            User.TryGetAccessTokenExpiresAtUtc());

        var result = await logoutCommandHandler.Handle(command, cancellationToken);

        return result.Match(
            _ =>
            {
                Response.Cookies.Delete(
                    authOptions.RefreshTokenCookieName,
                    AuthUtils.BuildRefreshTokenCookieOptions(Request, null));

                return NoContent();
            },
            error => resultHttpMapper.ToFailureActionResult(this, error));
    }

    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var userId = User.TryGetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        Request.Cookies.TryGetValue(authOptions.RefreshTokenCookieName, out var refreshToken);

        var command = new LogoutAllCommand(
            userId.Value,
            refreshToken,
            User.TryGetAccessTokenSessionId(),
            User.TryGetAccessTokenId(),
            User.TryGetAccessTokenExpiresAtUtc());

        var result = await logoutAllCommandHandler.Handle(command, cancellationToken);

        return result.Match(
            () =>
            {
                Response.Cookies.Delete(
                    authOptions.RefreshTokenCookieName,
                    AuthUtils.BuildRefreshTokenCookieOptions(Request, null));
                return NoContent();
            },
            error => resultHttpMapper.ToFailureActionResult(this, error));
    }

    [HttpGet("session")]
    public async Task<IActionResult> GetAuthSession(CancellationToken cancellationToken)
    {
        var userId = User.TryGetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var query = new GetAuthSessionQuery(userId.Value);
        var result = await getAuthSessionQueryHandler.Handle(query, cancellationToken);

        return result.Match(
            authSession => Ok(authSession.MapToGetAuthSessionResponse()),
            error => resultHttpMapper.ToFailureActionResult(this, error));
    }
}
