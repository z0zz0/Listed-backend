using Listed.Application.Contracts.CQRS;
using Listed.Application.Common;
using Listed.Application.Auth.Commands.Login;
using Listed.Application.Auth.Commands.Logout;
using Listed.Application.Auth.Commands.LogoutAll;
using Listed.Application.Auth.Commands.Refresh;
using Listed.Application.Auth.Queries.GetAuthSession;
using Listed.Application.Auth.Results;
using Listed.Application.Users.Commands.CompleteSignup;
using Listed.Application.Users.Commands.SaveSignupPersonalInfo;
using Listed.Application.Users.Commands.StartSignup;
using Listed.Application.Users.Commands.VerifySignupEmail;
using Listed.Application.Users.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Listed.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<LoginCommand, Result<AuthTokensResult>>, LoginCommandHandler>();
        services.AddScoped<ICommandHandler<RefreshCommand, Result<AuthTokensResult>>, RefreshCommandHandler>();
        services.AddScoped<ICommandHandler<LogoutCommand, Result<LogoutResult>>, LogoutCommandHandler>();
        services.AddScoped<ICommandHandler<LogoutAllCommand, Result>, LogoutAllCommandHandler>();
        services.AddScoped<IQueryHandler<GetAuthSessionQuery, Result<GetAuthSessionResult>>, GetAuthSessionQueryHandler>();

        services.AddScoped<ICommandHandler<StartSignupCommand, Result<StartSignupResult>>, StartSignupCommandHandler>();
        services.AddScoped<ICommandHandler<VerifySignupEmailCommand, Result<VerifySignupEmailResult>>, VerifySignupEmailCommandHandler>();
        services.AddScoped<ICommandHandler<SaveSignupPersonalInfoCommand, Result<SaveSignupPersonalInfoResult>>, SaveSignupPersonalInfoCommandHandler>();
        services.AddScoped<ICommandHandler<CompleteSignupCommand, Result<CompleteSignupResult>>, CompleteSignupCommandHandler>();

        return services;
    }
}
