using Listed.Application.Contracts.CQRS;
using Listed.Application.Common;
using Listed.Application.Commands.CreateUser;
using Microsoft.Extensions.DependencyInjection;

namespace Listed.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateUserCommand, Result<Guid>>, CreateUserCommandHandler>();

        return services;
    }
}
