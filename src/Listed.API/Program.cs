using Listed.API.Middleware;
using Serilog;

namespace Listed.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Host.UseSerilog();

        var app = builder.Build();

        app.UseMiddleware<CorrelationIdMiddleware>();

        app.MapGet("/", () => { Log.Information("Reached endpoint."); return "Hello World!"; });

        app.Run();
    }
}
