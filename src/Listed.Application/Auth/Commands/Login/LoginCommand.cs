using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Auth.Results;

namespace Listed.Application.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    Guid DeviceId,
    string? CurrentRefreshToken,
    string? IpAddress,
    string? UserAgent) : ICommand<Result<AuthTokensResult>>;
