namespace BankingSystem.API.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddBankingSystemHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var hangfireConnection = configuration.GetConnectionString("HangfireConnection");

        var healthChecksBuilder = services.AddHealthChecks();

        // Only add database health checks if not using in-memory database
        if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("InMemoryDatabase"))
        {
            healthChecksBuilder
                .AddNpgSql(
                    connectionString!,
                    name: "database",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "db", "sql", "postgresql" },
                    timeout: TimeSpan.FromSeconds(3))
                .AddNpgSql(
                    hangfireConnection ?? connectionString!,
                    name: "hangfire",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "hangfire", "jobs" },
                    timeout: TimeSpan.FromSeconds(3));
        }

        healthChecksBuilder
            // Memory health check
            .AddCheck("memory", () =>
            {
                var allocated = GC.GetTotalMemory(forceFullCollection: false);
                var threshold = 1024L * 1024L * 1024L; // 1 GB

                return allocated < threshold
                    ? HealthCheckResult.Healthy($"Memory usage: {allocated / 1024 / 1024} MB")
                    : HealthCheckResult.Degraded($"High memory usage: {allocated / 1024 / 1024} MB");
            }, tags: new[] { "memory", "resource", "ready", "live" })

            // Application health check
            .AddCheck("self", () => HealthCheckResult.Healthy("API is running"), 
                tags: new[] { "self", "ready", "live" });

        return services;
    }
}
