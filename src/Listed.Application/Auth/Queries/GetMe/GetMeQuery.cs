using Listed.Application.Auth.Results;
using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;

namespace Listed.Application.Auth.Queries.GetMe;

public sealed record GetMeQuery(Guid UserId) : IQuery<Result<GetMeResult>>;
