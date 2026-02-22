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
    private readonly ValkeyTestDatabase _valkey = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ListedDatabase"] = _database.ConnectionString,
                ["ConnectionStrings:Valkey"] = _valkey.ConnectionString,
                ["Auth:SigningKey"] = "listed-integration-tests-signing-key-123456789",
                ["DataProtection:ApplicationName"] = "listed-api-integration-tests",
                ["DataProtection:KeyRingKey"] = "listed:dpkeys:integration-tests",
                ["DataProtection:CertificatePath"] = string.Empty,
                ["DataProtection:CertificatePassword"] = string.Empty
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        await _valkey.StartAsync();

        Environment.SetEnvironmentVariable(
            "ConnectionStrings__ListedDatabase",
            _database.ConnectionString);
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Valkey",
            _valkey.ConnectionString);
        Environment.SetEnvironmentVariable(
            "Auth__SigningKey",
            "listed-integration-tests-signing-key-123456789");
        Environment.SetEnvironmentVariable(
            "DataProtection__ApplicationName",
            "listed-api-integration-tests");
        Environment.SetEnvironmentVariable(
            "DataProtection__KeyRingKey",
            "listed:dpkeys:integration-tests");
        Environment.SetEnvironmentVariable(
            "DataProtection__CertificatePath",
            string.Empty);
        Environment.SetEnvironmentVariable(
            "DataProtection__CertificatePassword",
            string.Empty);

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
        Environment.SetEnvironmentVariable("ConnectionStrings__Valkey", null);
        Environment.SetEnvironmentVariable("Auth__SigningKey", null);
        Environment.SetEnvironmentVariable("DataProtection__ApplicationName", null);
        Environment.SetEnvironmentVariable("DataProtection__KeyRingKey", null);
        Environment.SetEnvironmentVariable("DataProtection__CertificatePath", null);
        Environment.SetEnvironmentVariable("DataProtection__CertificatePassword", null);
        await _valkey.DisposeAsync();
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }
}
