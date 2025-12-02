namespace BankingSystem.API.Middleware;

using System.Text;

/// <summary>
/// Middleware to sanitize input data to prevent XSS attacks
/// </summary>
public class InputSanitizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputSanitizationMiddleware> _logger;

    public InputSanitizationMiddleware(RequestDelegate next, ILogger<InputSanitizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only sanitize POST, PUT, PATCH requests
        if (HttpMethods.IsPost(context.Request.Method) ||
            HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsPatch(context.Request.Method))
        {
            // Enable buffering to read request body
            context.Request.EnableBuffering();

            // Read the original body
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var originalBody = await reader.ReadToEndAsync();

            // Sanitize the body
            var sanitizedBody = SanitizeInput(originalBody);

            // If body was modified, create new request body
            if (!string.Equals(originalBody, sanitizedBody, StringComparison.Ordinal))
            {
                _logger.LogWarning("Potentially malicious input detected and sanitized in request to {Path}",
                    context.Request.Path);

                var sanitizedBytes = Encoding.UTF8.GetBytes(sanitizedBody);
                context.Request.Body = new MemoryStream(sanitizedBytes);
                context.Request.Body.Position = 0;

                // Update content length
                context.Request.ContentLength = sanitizedBytes.Length;
            }
            else
            {
                // Reset position for next middleware
                context.Request.Body.Position = 0;
            }
        }

        await _next(context);
    }

    private static string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove potential XSS vectors
        var sanitized = input;

        // Remove script tags
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"<script[^>]*>.*?</script>",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.Singleline);

        // Remove javascript: protocol
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"javascript:",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove vbscript: protocol
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"vbscript:",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove data: URLs that might contain scripts
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"data:text/html[^,""]*,",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove event handlers (onclick, onload, etc.)
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"\s+on\w+\s*=",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Encode dangerous characters in JSON strings
        sanitized = SanitizeJsonStrings(sanitized);

        return sanitized;
    }

    private static string SanitizeJsonStrings(string json)
    {
        try
        {
            // Parse JSON and sanitize string values
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var sanitized = SanitizeJsonElement(doc.RootElement);
            return System.Text.Json.JsonSerializer.Serialize(sanitized);
        }
        catch
        {
            // If JSON parsing fails, return original (don't break valid requests)
            return json;
        }
    }

    private static object SanitizeJsonElement(System.Text.Json.JsonElement element)
    {
        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.String:
                var stringValue = element.GetString();
                if (stringValue != null)
                {
                    // HTML encode dangerous characters
                    stringValue = System.Web.HttpUtility.HtmlEncode(stringValue);
                }
                return stringValue;

            case System.Text.Json.JsonValueKind.Array:
                var array = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    array.Add(SanitizeJsonElement(item));
                }
                return array;

            case System.Text.Json.JsonValueKind.Object:
                var obj = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    obj[property.Name] = SanitizeJsonElement(property.Value);
                }
                return obj;

            default:
                return element;
        }
    }
}

/// <summary>
/// Extension method to register InputSanitizationMiddleware
/// </summary>
public static class InputSanitizationMiddlewareExtensions
{
    public static IApplicationBuilder UseInputSanitization(this IApplicationBuilder app)
    {
        return app.UseMiddleware<InputSanitizationMiddleware>();
    }
}