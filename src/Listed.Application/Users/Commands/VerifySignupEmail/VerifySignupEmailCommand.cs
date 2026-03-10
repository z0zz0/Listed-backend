using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Users.Results;

namespace Listed.Application.Users.Commands.VerifySignupEmail;

public sealed record VerifySignupEmailCommand(Guid SignupId, string VerificationCode) : ICommand<Result<VerifySignupEmailResult>>;
