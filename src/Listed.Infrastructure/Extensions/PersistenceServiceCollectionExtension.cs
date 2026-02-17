using Listed.Application.Contracts.Persistence;
using Listed.Application.Contracts.Security;
using Listed.Domain.Enums;
using Listed.Infrastructure.Persistence;
using Listed.Infrastructure.Persistence.Repositories;
using Listed.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Listed.Infrastructure.Extensions;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string connectionString
    )
    {
        services.AddDbContext<ListedDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOpts => npgsqlOpts
                .MapEnum<EventStatus>("event_status", "listed")
                .MapEnum<OrganisationRole>("organisation_role", "listed")
                .MapEnum<ParticipationStatus>("participation_status", "listed")
                .MigrationsHistoryTable("__EFMigrationsHistory", "listed")
            )
        );

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        return services;
    }
}
