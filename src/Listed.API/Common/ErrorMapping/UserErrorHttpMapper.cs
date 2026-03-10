using Listed.API.Contracts;
using Listed.Application.Common;
using Listed.Application.Users.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Listed.API.Common.ErrorMapping;

public sealed class UserErrorHttpMapper : IErrorHttpMapper
{
    public bool CanHandle(string errorCode)
    {
        return errorCode == UserError.EmailAlreadyInUseCode
               || UserError.IsValidationCode(errorCode)
               || UserError.IsRateLimitCode(errorCode)
               || UserError.IsInternalCode(errorCode);
    }

    public IActionResult Map(ControllerBase controller, Error error)
    {
        if (error.Code == UserError.EmailAlreadyInUseCode)
        {
            return controller.Conflict(new { error.Code, error.Message });
        }

        if (UserError.IsValidationCode(error.Code))
        {
            return controller.BadRequest(new { error.Code, error.Message });
        }

        if (UserError.IsRateLimitCode(error.Code))
        {
            return controller.StatusCode(StatusCodes.Status429TooManyRequests, new { error.Code, error.Message });
        }

        if (UserError.IsInternalCode(error.Code))
        {
            return controller.StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message });
        }

        return controller.BadRequest(new { error.Code, error.Message });
    }
}
