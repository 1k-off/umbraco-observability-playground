using Serilog.Context;

namespace UmbracoMonitoring.Middleware;

public class RequestIdMiddleware
{
    private const string RequestIdHeader = "X-Request-Id";
    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        string requestId = context.Request.Headers.TryGetValue(RequestIdHeader, out Microsoft.Extensions.Primitives.StringValues header)
            && !string.IsNullOrWhiteSpace(header)
            ? header.ToString()
            : context.TraceIdentifier;

        context.TraceIdentifier = requestId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[RequestIdHeader] = requestId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("requestId", requestId))
        {
            await _next(context);
        }
    }
}
