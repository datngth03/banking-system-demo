namespace BankingSystem.API.Middleware;

/// <summary>
/// Middleware to add security headers to HTTP responses
/// Implements OWASP recommendations for secure headers
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // X-Content-Type-Options: Prevents MIME type sniffing
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // X-Frame-Options: Prevents clickjacking attacks
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // X-XSS-Protection: Enable XSS filter in older browsers
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // Referrer-Policy: Control referrer information
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Permissions-Policy: Control browser features
        context.Response.Headers.Append("Permissions-Policy",
            "camera=(), microphone=(), geolocation=(), payment=()");

        // Content-Security-Policy: Mitigate XSS and injection attacks
        // Allow inline scripts/styles for Swagger UI or during Development only
        var allowInlineForSwagger = _environment.IsDevelopment() || context.Request.Path.StartsWithSegments("/swagger");

        var scriptSrc = allowInlineForSwagger ? "script-src 'self' 'unsafe-inline'" : "script-src 'self'";
        var styleSrc = "style-src 'self' 'unsafe-inline'"; // keep unsafe-inline for styles

        var cspDirectives = new[]
        {
            "default-src 'self'",
            scriptSrc,
            styleSrc,
            "img-src 'self' data: https:",
            "font-src 'self'",
            "connect-src 'self'",
            "frame-ancestors 'none'",
            "base-uri 'self'",
            "form-action 'self'"
        };

        context.Response.Headers.Append("Content-Security-Policy", string.Join("; ", cspDirectives));

        // HSTS: Force HTTPS (only add in production and when using HTTPS)
        if (!_environment.IsDevelopment() && context.Request.IsHttps)
        {
            context.Response.Headers.Append("Strict-Transport-Security",
                "max-age=31536000; includeSubDomains; preload");
        }

        // Remove server header to hide implementation details
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");

        await _next(context);
    }
}

/// <summary>
/// Extension method to register SecurityHeadersMiddleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
