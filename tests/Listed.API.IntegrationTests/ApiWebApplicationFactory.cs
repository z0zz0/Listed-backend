using Listed.API;
using Listed.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Listed.Testing.Infrastructure;

namespace Listed.API.IntegrationTests;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgresTestDatabase _database = new("listed_test");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ListedDatabase"] = _database.ConnectionString
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__ListedDatabase",
            _database.ConnectionString);

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ListedDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ListedDbContext>();
                await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE listed.users RESTART IDENTITY CASCADE;");
                return;
            }
            catch (NpgsqlException) when (attempt < maxAttempts)
            {
                await Task.Delay(150 * attempt);
            }
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__ListedDatabase", null);
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }
}
