using Listed.API.Contracts;
using Listed.Application.Auth.Errors;
using Listed.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Listed.API.Common.ErrorMapping;

public sealed class AuthErrorHttpMapper : IErrorHttpMapper
{
    public bool CanHandle(string errorCode)
    {
        return errorCode == AuthError.UserNotFoundCode
               || errorCode == AuthError.TokenGenerationFailedCode
               || AuthError.IsValidationCode(errorCode)
               || AuthError.IsConflictCode(errorCode)
               || AuthError.IsUnauthorizedCode(errorCode);
    }

    public IActionResult Map(ControllerBase controller, Error error)
    {
        if (AuthError.IsValidationCode(error.Code))
        {
            return controller.BadRequest(new { error.Code, error.Message });
        }

        if (AuthError.IsUnauthorizedCode(error.Code))
        {
            return controller.Unauthorized(new { error.Code, error.Message });
        }

        if (AuthError.IsConflictCode(error.Code))
        {
            return controller.Conflict(new { error.Code, error.Message });
        }

        if (error.Code == AuthError.UserNotFoundCode)
        {
            return controller.NotFound(new { error.Code, error.Message });
        }

        if (error.Code == AuthError.TokenGenerationFailedCode)
        {
            return controller.StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message });
        }

        return controller.BadRequest(new { error.Code, error.Message });
    }
}
