using Listed.API.Middleware;
using Listed.API.Extensions;
using Listed.Application.Extensions;
using Listed.Infrastructure.Extensions;
using Serilog;
using StackExchange.Redis;

namespace Listed.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

        var valkeyConnectionString = builder.Configuration.GetConnectionString("Valkey")
            ?? throw new InvalidOperationException("Connection string 'Valkey' is missing.");

        var valkeyConnectionMultiplexer = ConnectionMultiplexer.Connect(valkeyConnectionString);

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration, valkeyConnectionMultiplexer);
        builder.Services.AddApi(builder.Configuration, valkeyConnectionMultiplexer);

        var app = builder.Build();
        app.UseForwardedHeaders();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging();
        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
