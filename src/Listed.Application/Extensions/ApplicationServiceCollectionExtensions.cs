using Listed.Application.Contracts.CQRS;
using Listed.Application.Common;
using Listed.Application.Users.Commands.CreateUser;
using Listed.Application.Users.Queries.GetUserByEmail;
using Listed.Application.Users.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Listed.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateUserCommand, Result<CreateUserResult>>, CreateUserCommandHandler>();
        services.AddScoped<IQueryHandler<GetUserByEmailQuery, Result<GetUserResult>>, GetUserByEmailQueryHandler>();

        return services;
    }
}
