using Listed.API.Middleware;
using Listed.API.Extensions;
using Listed.Application.Extensions;
using Listed.Infrastructure.Extensions;
using Serilog;

namespace Listed.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
        var connectionString = builder.Configuration.GetConnectionString("ListedDatabase")
            ?? throw new InvalidOperationException("Connection string 'ListedDatabase' is missing.");

        builder.Services.AddApplication();
        builder.Services.AddErrorMapping();
        builder.Services.AddPersistence(connectionString);
        builder.Services.AddControllers();

        var app = builder.Build();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging();
        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.MapControllers();
        app.MapGet("/", () => { Log.Information("Reached endpoint."); return "Hello World!"; });

        app.Run();
    }
}
