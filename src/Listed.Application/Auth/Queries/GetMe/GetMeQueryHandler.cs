using Listed.Application.Auth.Errors;
using Listed.Application.Auth.Results;
using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Contracts.Persistence;
using Microsoft.Extensions.Logging;

namespace Listed.Application.Auth.Queries.GetMe;

public sealed class GetMeQueryHandler(
    IUserAuthRepository userAuthRepository,
    ILogger<GetMeQueryHandler> logger) : IQueryHandler<GetMeQuery, Result<GetMeResult>>
{
    public async Task<Result<GetMeResult>> Handle(GetMeQuery query, CancellationToken cancellationToken)
    {
        var user = await userAuthRepository.GetByIdForAuthAsync(query.UserId, cancellationToken);
        if (user is null || user.AuthInfo is null)
        {
            logger.LogInformation("GetMe returned not found for UserId={UserId}", query.UserId);
            return Result<GetMeResult>.Failure(AuthError.UserNotFound(query.UserId));
        }

        logger.LogInformation(
            "GetMe succeeded. UserId={UserId}, Email={Email}, AuthVersion={AuthVersion}",
            user.Id,
            user.Email,
            user.AuthInfo.AuthVersion);

        return Result<GetMeResult>.Success(new GetMeResult(user.Id, user.Email, user.AuthInfo.AuthVersion));
    }
}
