using Listed.Application.Auth.Errors;
using Listed.Application.Auth.Results;
using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Contracts.Persistence;
using Microsoft.Extensions.Logging;

namespace Listed.Application.Auth.Queries.GetAuthSession;

public sealed class GetAuthSessionQueryHandler(
    IUserAuthRepository userAuthRepository,
    ILogger<GetAuthSessionQueryHandler> logger) : IQueryHandler<GetAuthSessionQuery, Result<GetAuthSessionResult>>
{
    public async Task<Result<GetAuthSessionResult>> Handle(GetAuthSessionQuery query, CancellationToken cancellationToken)
    {
        var user = await userAuthRepository.GetByIdForAuthAsync(query.UserId, cancellationToken);
        if (user is null || user.AuthInfo is null)
        {
            logger.LogInformation("GetAuthSession returned not found for UserId={UserId}", query.UserId);
            return Result<GetAuthSessionResult>.Failure(AuthError.SessionNotFound(query.UserId));
        }

        logger.LogInformation(
            "GetAuthSession succeeded. UserId={UserId}, Email={Email}, AuthVersion={AuthVersion}",
            user.Id,
            user.Email,
            user.AuthInfo.AuthVersion);

        return Result<GetAuthSessionResult>.Success(new GetAuthSessionResult(user.Id, user.Email, user.AuthInfo.AuthVersion));
    }
}
