using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;

namespace Listed.Application.Auth.Commands.LogoutAll;

public sealed record LogoutAllCommand(
    Guid UserId,
    string? AccessTokenId,
    DateTime? AccessTokenExpiresAtUtc) : ICommand<Result>;
