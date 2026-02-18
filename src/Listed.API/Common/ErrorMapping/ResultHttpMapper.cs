using Listed.API.Contracts;
using Listed.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Listed.API.Common.ErrorMapping;

public sealed class ResultHttpMapper(IEnumerable<IErrorHttpMapper> mappers)
{
    public IActionResult ToFailureActionResult(ControllerBase controller, Error error)
    {
        var mapper = mappers.FirstOrDefault(m => m.CanHandle(error.Code));
        if (mapper is not null)
        {
            return mapper.Map(controller, error);
        }

        return controller.BadRequest(new { error.Code, error.Message });
    }
}
