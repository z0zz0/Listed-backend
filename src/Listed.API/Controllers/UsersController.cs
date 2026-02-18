using Listed.Application.Contracts.CQRS;
using Listed.Application.Users.Commands.CreateUser;
using Listed.Application.Common;
using Listed.API.Common.ErrorMapping;
using Listed.API.Contracts.Users;
using Microsoft.AspNetCore.Mvc;

namespace Listed.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(
    ICommandHandler<CreateUserCommand, Result<Guid>> createUserCommandHandler,
    ResultHttpMapper resultHttpMapper) : ControllerBase
{
    [HttpPost(Name = "CreateUser")]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(request.Email, request.Password);
        var result = await createUserCommandHandler.Handle(command, cancellationToken);

        return result.Match(
            id => Created($"/api/users/{id}", new CreateUserResponse(id)),
            error => resultHttpMapper.ToFailureActionResult(this, error));
    }
}
