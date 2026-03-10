using Listed.Application.Auth.Results;
using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;

namespace Listed.Application.Auth.Queries.GetAuthSession;

public sealed record GetAuthSessionQuery(Guid UserId) : IQuery<Result<GetAuthSessionResult>>;
