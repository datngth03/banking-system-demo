namespace BankingSystem.Infrastructure.Monitoring;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BankingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Background service to update observable metrics periodically
/// </summary>
public class MetricsCollectorService : BackgroundService
{
    private readonly ILogger<MetricsCollectorService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BankingSystemMetrics _metrics;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public MetricsCollectorService(
        ILogger<MetricsCollectorService> logger,
        IServiceScopeFactory scopeFactory,
        BankingSystemMetrics metrics)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Metrics Collector Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectMetrics(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting metrics");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Metrics Collector Service stopped");
    }

    private async Task CollectMetrics(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BankingSystemDbContext>();

        // Collect active users count (logged in within last 24 hours)
        var activeUsersCount = await context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.LastLoginAt >= DateTime.UtcNow.AddHours(-24))
            .CountAsync(cancellationToken);

        // Collect total accounts count
        var totalAccountsCount = await context.Accounts
            .AsNoTracking()
            .Where(a => a.IsActive)
            .CountAsync(cancellationToken);

        // Collect pending transactions count (placeholder)
        var pendingTransactionsCount = 0L;

        // Update metrics
        _metrics.UpdateActiveUsers(activeUsersCount);
        _metrics.UpdateTotalAccounts(totalAccountsCount);
        _metrics.UpdatePendingTransactions(pendingTransactionsCount);

        _logger.LogDebug(
            "Metrics updated: Active Users={ActiveUsers}, Total Accounts={TotalAccounts}",
            activeUsersCount,
            totalAccountsCount);
    }
}