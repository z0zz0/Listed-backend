using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Auth.Results;

namespace Listed.Application.Auth.Commands.Logout;

public sealed record LogoutCommand(
    Guid UserId,
    string? RefreshToken,
    Guid? AccessTokenSessionId,
    string? AccessTokenId,
    DateTime? AccessTokenExpiresAtUtc) : ICommand<Result<LogoutResult>>;
