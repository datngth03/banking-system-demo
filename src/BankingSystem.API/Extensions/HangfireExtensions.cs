namespace BankingSystem.API.Extensions;

using BankingSystem.Infrastructure.BackgroundJobs;
using BankingSystem.Application.Constants;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for configuring Hangfire dashboard and recurring jobs
/// </summary>
public static class HangfireExtensions
{
    /// <summary>
    /// Configure Hangfire dashboard and recurring jobs
    /// </summary>
    public static IApplicationBuilder UseHangfireConfiguration(this IApplicationBuilder app)
    {
        // Enable Hangfire Dashboard with proper authorization
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[]
            {
                new HangfireDashboardAuthorizationFilter()
            },
            DashboardTitle = "BankingSystem Background Jobs",
            StatsPollingInterval = 5000, // Poll every 5 seconds
            DisplayStorageConnectionString = false // Don't show connection string for security
        });

        // Schedule recurring jobs
        ScheduleRecurringJobs();

        return app;
    }

    private static void ScheduleRecurringJobs()
    {
        // Apply interest monthly (on the 1st of each month at 00:00)
        RecurringJob.AddOrUpdate<InterestApplicationJob>(
            "apply-monthly-interest",
            job => job.ApplyMonthlyInterest(),
            Cron.Monthly(1, 0), // 1st day of month at midnight
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // Publish outbox messages every minute
        RecurringJob.AddOrUpdate<OutboxPublisherJob>(
            "publish-outbox-messages",
            job => job.PublishOutboxMessages(),
            Cron.Minutely(),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // Note: Hangfire automatically cleans up old jobs based on configuration
        // Default: succeeded jobs are kept for 24 hours
        // You can configure this in Hangfire settings if needed
    }
}

/// <summary>
/// Authorization filter for Hangfire Dashboard
/// Requires Admin role to access the dashboard
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Check if user is authenticated
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        // Check if user has Admin role
        var isAdmin = httpContext.User.IsInRole(Roles.Admin);

        // In development, you might want to allow Manager role as well
#if DEBUG
        var isManager = httpContext.User.IsInRole(Roles.Manager);
        return isAdmin || isManager;
#else
        return isAdmin;
#endif
    }
}
