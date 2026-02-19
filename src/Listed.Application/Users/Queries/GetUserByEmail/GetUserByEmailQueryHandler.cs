using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Contracts.Persistence;
using Listed.Application.Users.Common;
using Listed.Application.Users.Errors;
using Listed.Application.Users.Results;
using Microsoft.Extensions.Logging;

namespace Listed.Application.Users.Queries.GetUserByEmail;

public sealed class GetUserByEmailQueryHandler(
    IUserRepository userRepository,
    ILogger<GetUserByEmailQueryHandler> logger) : IQueryHandler<GetUserByEmailQuery, Result<GetUserResult>>
{
    public async Task<Result<GetUserResult>> Handle(GetUserByEmailQuery query, CancellationToken cancellationToken)
    {
        var emailResult = UserUtils.NormalizeAndValidateEmail(query.Email);
        if (emailResult.IsFailure)
        {
            logger.LogInformation("GetUserByEmail validation failed with error code {ErrorCode}. Email={Email}", 
            emailResult.Error.Code,
            query.Email);
            
            return Result<GetUserResult>.Failure(emailResult.Error);
        }

        var normalizedEmail = emailResult.Value!;

        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            logger.LogInformation("GetUserByEmail returned not found for Email={Email}", normalizedEmail);
            return Result<GetUserResult>.Failure(UserError.UserNotFoundByEmail(normalizedEmail));
        }

        logger.LogInformation(
            "GetUserByEmail succeeded. UserId={UserId}, Email={Email}, HasUserInfo={HasUserInfo}, PhotoCount={PhotoCount}",
            user.Id,
            user.Email,
            user.UserInfo is not null,
            user.Photos.Count);

        return Result<GetUserResult>.Success(user.MapToGetUserResult());
    }
}
