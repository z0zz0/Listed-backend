using Serilog.Context;

namespace Listed.API.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task Invoke(HttpContext context)
    {
        var headerValue = context.Request.Headers[HeaderName].FirstOrDefault();
        string correlationId = Guid.TryParse(headerValue, out var parsedGuid) ? parsedGuid.ToString() : Guid.NewGuid().ToString();

        context.Items[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
