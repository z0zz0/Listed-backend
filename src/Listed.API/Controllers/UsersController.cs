using Listed.Application.Contracts.CQRS;
using Listed.Application.Users.Commands.CreateUser;
using Listed.Application.Common;
using Listed.Application.Users.Queries.GetUserByEmail;
using Listed.Application.Users.Results;
using Listed.API.Common.Utils;
using Listed.API.Common.ErrorMapping;
using Listed.API.Contracts.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Listed.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(
    ICommandHandler<CreateUserCommand, Result<CreateUserResult>> createUserCommandHandler,
    IQueryHandler<GetUserByEmailQuery, Result<GetUserResult>> getUserByEmailQueryHandler,
    ResultHttpMapper resultHttpMapper) : ControllerBase
{
    [HttpGet("by-email", Name = "GetUserByEmail")]
    public async Task<IActionResult> GetUserByEmail([FromQuery, BindRequired] string email, CancellationToken cancellationToken)
    {
        var query = new GetUserByEmailQuery(email);
        var result = await getUserByEmailQueryHandler.Handle(query, cancellationToken);

        return result.Match(
            user => Ok(user.MapToGetUserResponse()),
            error => resultHttpMapper.ToFailureActionResult(this, error));
    }

    [HttpPost(Name = "CreateUser")]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(request.Email, request.Password);
        var result = await createUserCommandHandler.Handle(command, cancellationToken);

        return result.Match(
            createUserResult => Created($"/api/users/{createUserResult.Id}", new CreateUserResponse(createUserResult.Id, createUserResult.Email)),
            error => resultHttpMapper.ToFailureActionResult(this, error));
    }
}
