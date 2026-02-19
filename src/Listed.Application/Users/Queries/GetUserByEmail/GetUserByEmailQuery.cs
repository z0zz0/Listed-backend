using Listed.Application.Common;
using Listed.Application.Contracts.CQRS;
using Listed.Application.Users.Results;

namespace Listed.Application.Users.Queries.GetUserByEmail;

public sealed record GetUserByEmailQuery(string Email) : IQuery<Result<GetUserResult>>;
