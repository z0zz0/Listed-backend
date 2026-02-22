using Testcontainers.Redis;

namespace Listed.Testing.Infrastructure;

public sealed class ValkeyTestDatabase : IAsyncDisposable
{
    private readonly RedisContainer _container;

    public ValkeyTestDatabase()
    {
        _container = new RedisBuilder()
            .WithImage("valkey/valkey:8-alpine")
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public Task StartAsync()
    {
        return _container.StartAsync();
    }

    public ValueTask DisposeAsync()
    {
        return _container.DisposeAsync();
    }
}
