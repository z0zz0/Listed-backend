using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Users.Results;

namespace Listed.Application.Users.Commands.StartSignup;

public sealed record StartSignupCommand(string Email) : ICommand<Result<StartSignupResult>>;
