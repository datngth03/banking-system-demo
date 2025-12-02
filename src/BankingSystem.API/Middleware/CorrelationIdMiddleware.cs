namespace BankingSystem.API.Middleware;

using Serilog.Context;
using System.Diagnostics;

/// <summary>
/// Middleware to add correlation ID to all requests for distributed tracing
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get correlation ID from request header or generate new one
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                           ?? Activity.Current?.Id
                           ?? Guid.NewGuid().ToString();

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // Add to HttpContext.Items for easy access throughout the request pipeline
        context.Items["CorrelationId"] = correlationId;

        // Add to OpenTelemetry Activity
        Activity.Current?.SetTag("correlation_id", correlationId);
        Activity.Current?.SetTag("http.route", context.Request.Path);
        Activity.Current?.SetTag("http.method", context.Request.Method);

        // Add to Serilog context for structured logging
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        using (LogContext.PushProperty("UserAgent", context.Request.Headers.UserAgent.ToString()))
        {
            await _next(context);
        }
    }
}

/// <summary>
/// Extension method to register CorrelationIdMiddleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
