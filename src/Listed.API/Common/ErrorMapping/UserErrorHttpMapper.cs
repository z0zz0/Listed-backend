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
               || UserError.IsNotFoundCode(errorCode);
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

        if (UserError.IsNotFoundCode(error.Code))
        {
            return controller.NotFound(new { error.Code, error.Message });
        }

        return controller.BadRequest(new { error.Code, error.Message });
    }
}
