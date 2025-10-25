using Serilog.Context;
using System.Diagnostics;

namespace Listed.API.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-ID";

    public async Task Invoke(HttpContext context)
    {
        // Prefer W3C trace id (from traceparent if caller sent it)
        var correlationId = Activity.Current?.TraceId.ToString();

        // if ecosystem still sends X-Correlation-ID, keep it for display/debug
        correlationId ??= context.Request.Headers[Header].FirstOrDefault();

        // Final fallback
        correlationId ??= context.TraceIdentifier;

        // Echo back for clients
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[Header] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
            
    }
}
