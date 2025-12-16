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

        // ENVIRONMENT-BASED RATE LIMITING
        // Production values are strict for security
        // Development values are relaxed for testing
        var environment = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var isProduction = environment == "Production";
        var isStaging = environment == "Staging";
        
        // Determine appropriate limits based on environment
        var authLimit = isProduction ? 10 : (isStaging ? 50 : 100);
        var authQueue = isProduction ? 2 : (isStaging ? 10 : 20);
        var sensitiveLimit = isProduction ? 20 : (isStaging ? 50 : 100);
        var sensitiveQueue = isProduction ? 3 : (isStaging ? 10 : 20);
        var adminLimit = isProduction ? 30 : (isStaging ? 100 : 200);
        var adminQueue = isProduction ? 5 : (isStaging ? 20 : 50);
        var globalLimit = isProduction ? 200 : (isStaging ? 500 : 1000);
        var globalQueue = isProduction ? 10 : (isStaging ? 20 : 50);
        
        services.AddRateLimiter(options =>
        {
            // Auth rate limiting - protect login/register endpoints
            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.PermitLimit = authLimit;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = authQueue;
            });

            // Normal rate limiting for general API endpoints
            options.AddFixedWindowLimiter("api", opt =>
            {
                opt.PermitLimit = rateLimitSettings.PermitLimit;
                opt.Window = TimeSpan.FromSeconds(rateLimitSettings.WindowInSeconds);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = rateLimitSettings.QueueLimit;
            });

            // Sensitive operations - money transfers, withdrawals
            options.AddFixedWindowLimiter("sensitive", opt =>
            {
                opt.PermitLimit = sensitiveLimit;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = sensitiveQueue;
            });

            // Admin operations
            options.AddFixedWindowLimiter("admin", opt =>
            {
                opt.PermitLimit = adminLimit;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = adminQueue;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Global fallback - per-IP protection
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(clientId, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = globalLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = globalQueue
                });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests",
                    message = "Rate limit exceeded. Please try again later.",
                    environment = environment,
                    retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? retryAfter.ToString()
                        : "60 seconds"
                }, cancellationToken);
            };
        });

        return services;
    }
}
