using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;

namespace Listed.Application.Auth.Commands.LogoutAll;

public sealed record LogoutAllCommand(
    Guid UserId,
    string? RefreshToken,
    Guid? AccessTokenSessionId,
    string? AccessTokenId,
    DateTime? AccessTokenExpiresAtUtc) : ICommand<Result>;
