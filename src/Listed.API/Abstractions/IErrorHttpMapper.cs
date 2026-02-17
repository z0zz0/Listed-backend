using Listed.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Listed.API.Abstractions;

public interface IErrorHttpMapper
{
    bool CanHandle(string errorCode);
    IActionResult Map(ControllerBase controller, Error error);
}
