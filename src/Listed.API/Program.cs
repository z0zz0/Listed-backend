using Listed.API.Middleware;
using Serilog;

namespace Listed.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));


        var app = builder.Build();

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.MapGet("/", () => { Log.Information("Reached endpoint."); return "Hello World!"; });

        app.Run();
    }
}
