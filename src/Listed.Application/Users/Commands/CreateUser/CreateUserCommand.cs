using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;

namespace Listed.Application.Users.Commands.CreateUser;

public sealed record CreateUserCommand(string Email, string Password) : ICommand<Result<Guid>>;
