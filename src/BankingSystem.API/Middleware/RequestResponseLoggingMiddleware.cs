namespace BankingSystem.API.Middleware;

using System.Diagnostics;
using System.Text;

/// <summary>
/// Middleware to log all HTTP requests and responses with timing
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    // Paths to skip logging (static files, health checks, etc.)
    private static readonly string[] SkipPaths = new[]
    {
        "/swagger/",
        "/swagger/v1/swagger.json",
        "/_framework/",
        "/_vs/",
        "/favicon.ico",
        "/health"
    };

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip all Swagger requests (UI + JSON + assets)
        if (context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/_framework") ||
            context.Request.Path.StartsWithSegments("/_vs") ||
            context.Request.Path.StartsWithSegments("/favicon.ico") ||
            context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        // Start timing
        var stopwatch = Stopwatch.StartNew();

        // Log request
        await LogRequest(context);

        // Capture original response body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Execute the request
            await _next(context);

            stopwatch.Stop();

            // Log response
            await LogResponse(context, stopwatch.ElapsedMilliseconds);

            // Copy response back to original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private static bool ShouldSkipLogging(PathString path)
    {
        return SkipPaths.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
    }

    private async Task LogRequest(HttpContext context)
    {
        var request = context.Request;

        var requestLog = new StringBuilder();
        requestLog.AppendLine("=== HTTP REQUEST ===");
        requestLog.AppendLine($"Method: {request.Method}");
        requestLog.AppendLine($"Path: {request.Path}");
        requestLog.AppendLine($"QueryString: {request.QueryString}");
        requestLog.AppendLine($"IP: {context.Connection.RemoteIpAddress}");

        // Log headers (excluding sensitive ones)
        var headers = request.Headers
            .Where(h => !IsSensitiveHeader(h.Key))
            .Select(h => $"{h.Key}: {h.Value}");
        requestLog.AppendLine($"Headers: {string.Join(", ", headers)}");

        // Log request body for non-GET requests (with size limit)
        if (request.Method != "GET" && request.ContentLength > 0 && request.ContentLength < 10000)
        {
            request.EnableBuffering();
            var body = await ReadStreamAsync(request.Body);
            request.Body.Position = 0;

            // Mask sensitive data
            var maskedBody = MaskSensitiveData(body);
            requestLog.AppendLine($"Body: {maskedBody}");
        }

        _logger.LogInformation(requestLog.ToString());
    }

    private async Task LogResponse(HttpContext context, long elapsedMs)
    {
        var response = context.Response;

        var responseLog = new StringBuilder();
        responseLog.AppendLine("=== HTTP RESPONSE ===");
        responseLog.AppendLine($"StatusCode: {response.StatusCode}");
        responseLog.AppendLine($"Duration: {elapsedMs}ms");

        // Log response body (with size limit)
        if (response.Body.CanSeek && response.Body.Length < 10000)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var body = await ReadStreamAsync(response.Body);
            response.Body.Seek(0, SeekOrigin.Begin);

            responseLog.AppendLine($"Body: {body}");
        }

        var logLevel = response.StatusCode >= 500 ? LogLevel.Error
                     : response.StatusCode >= 400 ? LogLevel.Warning
                     : LogLevel.Information;

        _logger.Log(logLevel, responseLog.ToString());

        // Log slow requests
        if (elapsedMs > 3000)
        {
            _logger.LogWarning("SLOW REQUEST: {Method} {Path} took {Duration}ms",
                context.Request.Method,
                context.Request.Path,
                elapsedMs);
        }
    }

    private static async Task<string> ReadStreamAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private static bool IsSensitiveHeader(string headerName)
    {
        var sensitiveHeaders = new[] { "Authorization", "Cookie", "X-API-Key" };
        return sensitiveHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase);
    }

    private static string MaskSensitiveData(string body)
    {
        // Mask password fields
        if (body.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            body = System.Text.RegularExpressions.Regex.Replace(
                body,
                @"""password""\s*:\s*""[^""]*""",
                @"""password"":""***MASKED***""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // Mask CVV
        if (body.Contains("cvv", StringComparison.OrdinalIgnoreCase))
        {
            body = System.Text.RegularExpressions.Regex.Replace(
                body,
                @"""cvv""\s*:\s*""[^""]*""",
                @"""cvv"":""***""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return body;
    }
}

/// <summary>
/// Extension method to register RequestResponseLoggingMiddleware
/// </summary>
public static class RequestResponseLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}
