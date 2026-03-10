using Listed.Application.Contracts.Signup;
using Listed.Infrastructure.Signup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Listed.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IConnectionMultiplexer valkeyConnectionMultiplexer)
    {
        var listedDatabaseConnectionString = configuration.GetConnectionString("ListedDatabase")
            ?? throw new InvalidOperationException("Connection string 'ListedDatabase' is missing.");

        services.AddSingleton<IConnectionMultiplexer>(valkeyConnectionMultiplexer);
        services.AddPersistence(listedDatabaseConnectionString);
        services.AddSecurity(configuration);
        services.AddEmail(configuration);
        services.AddScoped<ISignupVerificationStore, ValkeySignupVerificationStore>();

        return services;
    }
}
