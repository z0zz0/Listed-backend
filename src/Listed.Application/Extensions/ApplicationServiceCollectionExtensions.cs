using Listed.Application.Contracts.CQRS;
using Listed.Application.Common;
using Listed.Application.Auth.Commands.Login;
using Listed.Application.Auth.Commands.Logout;
using Listed.Application.Auth.Commands.LogoutAll;
using Listed.Application.Auth.Commands.Refresh;
using Listed.Application.Auth.Queries.GetMe;
using Listed.Application.Auth.Results;
using Listed.Application.Users.Commands.CreateUser;
using Listed.Application.Users.Queries.GetUserByEmail;
using Listed.Application.Users.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Listed.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<LoginCommand, Result<AuthTokensResult>>, LoginCommandHandler>();
        services.AddScoped<ICommandHandler<RefreshCommand, Result<AuthTokensResult>>, RefreshCommandHandler>();
        services.AddScoped<ICommandHandler<LogoutCommand, Result>, LogoutCommandHandler>();
        services.AddScoped<ICommandHandler<LogoutAllCommand, Result>, LogoutAllCommandHandler>();
        services.AddScoped<IQueryHandler<GetMeQuery, Result<GetMeResult>>, GetMeQueryHandler>();

        services.AddScoped<ICommandHandler<CreateUserCommand, Result<CreateUserResult>>, CreateUserCommandHandler>();
        services.AddScoped<IQueryHandler<GetUserByEmailQuery, Result<GetUserResult>>, GetUserByEmailQueryHandler>();

        return services;
    }
}
