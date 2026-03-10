using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Users.Results;

namespace Listed.Application.Users.Commands.CompleteSignup;

public sealed record CompleteSignupCommand(
    Guid SignupId,
    string Password) : ICommand<Result<CompleteSignupResult>>;
