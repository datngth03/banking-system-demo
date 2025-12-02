namespace BankingSystem.API.Middleware;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

/// <summary>
/// Custom health check response writer with detailed status
/// </summary>
public static class HealthCheckResponseWriter
{
    public static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var result = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration,
                exception = entry.Value.Exception?.Message,
                data = entry.Value.Data
            })
        };

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(result, options));
    }
}