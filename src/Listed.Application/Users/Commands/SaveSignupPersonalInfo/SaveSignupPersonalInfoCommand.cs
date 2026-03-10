using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Users.Results;

namespace Listed.Application.Users.Commands.SaveSignupPersonalInfo;

public sealed record SaveSignupPersonalInfoCommand(
    Guid SignupId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth) : ICommand<Result<SaveSignupPersonalInfoResult>>;
