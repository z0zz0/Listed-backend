using Listed.Domain.Enums;
using Listed.Infrastructure.Persistence;
using Listed.Testing.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Listed.Infrastructure.IntegrationTests;

public sealed class InfrastructureDatabaseFixture : IAsyncLifetime
{
    private readonly PostgresTestDatabase _database = new("listed_infra_test");

    private DbContextOptions<ListedDbContext>? _dbContextOptions;

    public ListedDbContext CreateDbContext()
    {
        if (_dbContextOptions is null)
        {
            throw new InvalidOperationException("Fixture is not initialized.");
        }

        return new ListedDbContext(_dbContextOptions);
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        _dbContextOptions = new DbContextOptionsBuilder<ListedDbContext>()
            .UseNpgsql(_database.ConnectionString, npgsqlOpts => npgsqlOpts
                .MapEnum<EventStatus>("event_status", "listed")
                .MapEnum<OrganisationRole>("organisation_role", "listed")
                .MapEnum<ParticipationStatus>("participation_status", "listed")
                .MigrationsHistoryTable("__EFMigrationsHistory", "listed"))
            .Options;

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task ResetAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE listed.users RESTART IDENTITY CASCADE;");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
    }
}
