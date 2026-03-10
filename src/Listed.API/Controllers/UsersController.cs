using Listed.Application.Contracts.CQRS;
using Listed.Application.Auth.Commands.Login;
using Listed.Application.Auth.Results;
using Listed.Application.Users.Commands.CompleteSignup;
using Listed.Application.Users.Commands.SaveSignupPersonalInfo;
using Listed.Application.Users.Commands.StartSignup;
using Listed.Application.Users.Commands.VerifySignupEmail;
using Listed.Application.Common;
using Listed.Application.Users.Results;
using Listed.API.Common.Utils;
using Listed.API.Common.ErrorMapping;
using Listed.API.Contracts.Users;
using Listed.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Listed.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(
    ICommandHandler<LoginCommand, Result<AuthTokensResult>> loginCommandHandler,
    ICommandHandler<StartSignupCommand, Result<StartSignupResult>> startSignupCommandHandler,
    ICommandHandler<VerifySignupEmailCommand, Result<VerifySignupEmailResult>> verifySignupEmailCommandHandler,
    ICommandHandler<SaveSignupPersonalInfoCommand, Result<SaveSignupPersonalInfoResult>> saveSignupPersonalInfoCommandHandler,
    ICommandHandler<CompleteSignupCommand, Result<CompleteSignupResult>> completeSignupCommandHandler,
    ResultHttpMapper resultHttpMapper,
    AuthOptions authOptions) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("signup/start", Name = "StartSignup")]
    public async Task<IActionResult> StartSignup(
        [FromBody] StartSignupRequest request,
        CancellationToken cancellationToken)
    {
        var command = new StartSignupCommand(request.Email);
        var result = await startSignupCommandHandler.Handle(command, cancellationToken);

        return result.Match(
            startSignupResult => Ok(new StartSignupResponse(startSignupResult.SignupId, startSignupResult.Email, startSignupResult.CodeExpiresAtUtc)),
            error => resultHttpMapper.ToFailureActionResult(this, error));
    }

    [AllowAnonymous]
    [HttpPost("signup/verify-code", Name = "VerifySignupEmail")]
    public async Task<IActionResult> VerifySignupEmail(
        [FromBody] VerifySignupEmailRequest request,
        CancellationToken cancellationToken)
    {
        var command = new VerifySignupEmailCommand(request.SignupId, request.VerificationCode);
        var result = await verifySignupEmailCommandHandler.Handle(command, cancellationToken);

        return result.Match(
            verifySignupEmailResult => Ok(new VerifySignupEmailResponse(verifySignupEmailResult.SignupId, verifySignupEmailResult.VerifiedAtUtc)),
            error => resultHttpMapper.ToFailureActionResult(this, error));
    }

    [AllowAnonymous]
    [HttpPost("signup/personal-info", Name = "SaveSignupPersonalInfo")]
    public async Task<IActionResult> SaveSignupPersonalInfo(
        [FromBody] SaveSignupPersonalInfoRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SaveSignupPersonalInfoCommand(
            request.SignupId,
            request.FirstName,
            request.LastName,
            request.DateOfBirth);

        var result = await saveSignupPersonalInfoCommandHandler.Handle(command, cancellationToken);

        return result.Match(
            saveSignupPersonalInfoResult => Ok(new SaveSignupPersonalInfoResponse(saveSignupPersonalInfoResult.SignupId)),
            error => resultHttpMapper.ToFailureActionResult(this, error));
    }

    [AllowAnonymous]
    [HttpPost("signup/complete", Name = "CompleteSignup")]
    public async Task<IActionResult> CompleteSignup(
        [FromBody] CompleteSignupRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CompleteSignupCommand(request.SignupId, request.Password);
        var result = await completeSignupCommandHandler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return resultHttpMapper.ToFailureActionResult(this, result.Error);
        }

        var completeSignupResult = result.Value!;
        var deviceId = AuthUtils.GetOrCreateDeviceId(Request, Response);
        Request.Cookies.TryGetValue(authOptions.RefreshTokenCookieName, out var currentRefreshToken);

        var loginCommand = new LoginCommand(
            completeSignupResult.Email,
            request.Password,
            deviceId,
            currentRefreshToken,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var loginResult = await loginCommandHandler.Handle(loginCommand, cancellationToken);
        if (loginResult.IsFailure)
        {
            return resultHttpMapper.ToFailureActionResult(this, loginResult.Error);
        }

        var tokens = loginResult.Value!;
        
        Response.Cookies.Append(
            authOptions.RefreshTokenCookieName,
            tokens.RefreshToken,
            AuthUtils.BuildRefreshTokenCookieOptions(Request, tokens.RefreshTokenExpiresAtUtc));

        return Created(
            $"/api/users/{completeSignupResult.Id}",
            new CompleteSignupResponse(
                completeSignupResult.Id,
                completeSignupResult.Email,
                tokens.AccessToken.MapToAccessTokenResponse()));
    }
}
