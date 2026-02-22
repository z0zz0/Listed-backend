using Listed.Application.Auth.Results;
using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;

namespace Listed.Application.Auth.Commands.Refresh;

public sealed record RefreshCommand(
    string? RefreshToken,
    Guid? DeviceId,
    string? IpAddress,
    string? UserAgent) : ICommand<Result<AuthTokensResult>>;
