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

        services.AddRateLimiter(options =>
        {
            // Strict rate limiting for authentication endpoints
            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.PermitLimit = 10; // Only 10 requests
                opt.Window = TimeSpan.FromMinutes(1); // Per minute
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 2; // Small queue
            });

            // Normal rate limiting for general API endpoints
            options.AddFixedWindowLimiter("api", opt =>
            {
                opt.PermitLimit = rateLimitSettings.PermitLimit;
                opt.Window = TimeSpan.FromSeconds(rateLimitSettings.WindowInSeconds);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = rateLimitSettings.QueueLimit;
            });

            // Strict rate limiting for sensitive operations (transfers, withdrawals)
            options.AddFixedWindowLimiter("sensitive", opt =>
            {
                opt.PermitLimit = 20; // 20 requests
                opt.Window = TimeSpan.FromMinutes(1); // Per minute
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 3;
            });

            // Very strict for admin operations
            options.AddFixedWindowLimiter("admin", opt =>
            {
                opt.PermitLimit = 30;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 5;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Global fallback
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(clientId, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 200, // Global limit per IP
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
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
