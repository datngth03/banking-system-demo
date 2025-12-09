namespace BankingSystem.API.Extensions;

using BankingSystem.Application.Models;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

public static class RateLimitExtensions
{
    public static IServiceCollection AddBankingSystemRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rateLimitSettings = configuration.GetSection("RateLimitSettings").Get<RateLimitSettings>()
            ?? new RateLimitSettings();

        // NOTE: These rate limits are configured for DEVELOPMENT/TESTING
        // For PRODUCTION, reduce these values significantly:
        // - auth: 10 req/min (currently 100)
        // - sensitive: 20 req/min (currently 100)
        // - admin: 30 req/min (currently 200)
        // - global: 200 req/min (currently 1000)
        
        services.AddRateLimiter(options =>
        {
            // Auth rate limiting - increased for development/testing
            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.PermitLimit = 100; // Increased from 10 to 100
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 20; // Increased from 2 to 20
            });

            // Normal rate limiting for general API endpoints
            options.AddFixedWindowLimiter("api", opt =>
            {
                opt.PermitLimit = rateLimitSettings.PermitLimit;
                opt.Window = TimeSpan.FromSeconds(rateLimitSettings.WindowInSeconds);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = rateLimitSettings.QueueLimit;
            });

            // Sensitive operations - increased for testing
            options.AddFixedWindowLimiter("sensitive", opt =>
            {
                opt.PermitLimit = 100; // Increased from 20 to 100
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 20; // Increased from 3 to 20
            });

            // Admin operations - increased for testing
            options.AddFixedWindowLimiter("admin", opt =>
            {
                opt.PermitLimit = 200; // Increased from 30 to 200
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 50; // Increased from 5 to 50
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Global fallback - significantly increased for load testing
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(clientId, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 1000, // Increased from 200 to 1000 for load testing
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 50 // Increased from 10 to 50
                });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests",
                    message = "Rate limit exceeded. Please try again later.",
                    retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? retryAfter.ToString()
                        : "60 seconds"
                }, cancellationToken);
            };
        });

        return services;
    }
}
