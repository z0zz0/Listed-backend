using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;

namespace Listed.Application.Auth.Commands.Logout;

public sealed record LogoutCommand(
    Guid UserId,
    string? RefreshToken,
    string? AccessTokenId,
    DateTime? AccessTokenExpiresAtUtc) : ICommand<Result>;
