using Listed.API.Middleware;
using Listed.Infrastructure.Persistence;
using Serilog;
using Microsoft.EntityFrameworkCore;

namespace Listed.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
        builder.Services.AddDbContext<ListedDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("ListedDatabase");
            options.UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention();
        }); 


        var app = builder.Build();

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.MapGet("/", () => { Log.Information("Reached endpoint."); return "Hello World!"; });

        app.Run();
    }
}
