namespace BankingSystem.Infrastructure.Services;

using BankingSystem.Application.Interfaces;
using BankingSystem.Infrastructure.Monitoring;
using System.Threading;

/// <summary>
/// Application-facing metrics service that wraps the low-level BankingSystemMetrics.
/// This keeps Application layer depending only on IMetricsService (abstraction).
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly BankingSystemMetrics _metrics;
    private long _activeUsersCounter;

    public MetricsService(BankingSystemMetrics metrics)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _activeUsersCounter = 0;
    }

    public void RecordTransaction(string transactionType, decimal amount, bool isSuccess)
    {
        _metrics.RecordTransaction(transactionType ?? "unknown", (double)amount, isSuccess);
    }

    public void RecordAccountOperation(string operationType, bool isSuccess)
    {
        // Map generic operation to account created metric for now if it's a create
        if (!string.IsNullOrEmpty(operationType) && operationType.Equals("create", StringComparison.OrdinalIgnoreCase))
        {
            _metrics.RecordAccountCreated("create", 0.0);
        }
        else
        {
            // fallback: increment a generic account operation counter via RecordAccountCreated with zero
            _metrics.RecordAccountCreated(operationType ?? "operation", 0.0);
        }
    }

    public void RecordAuthentication(bool isSuccess, string? failureReason = null)
    {
        _metrics.RecordLoginAttempt(isSuccess);
    }

    public void RecordApiDuration(string endpoint, double durationMs)
    {
        _metrics.RecordApiDuration(endpoint ?? "unknown", durationMs);
    }

    public void RecordDatabaseQueryDuration(string queryType, double durationMs)
    {
        _metrics.RecordDatabaseQueryDuration(queryType ?? "unknown", durationMs);
    }

    public void IncrementActiveUsers()
    {
        var newVal = Interlocked.Increment(ref _activeUsersCounter);
        _metrics.UpdateActiveUsers(newVal);
    }

    public void DecrementActiveUsers()
    {
        var newVal = Interlocked.Decrement(ref _activeUsersCounter);
        if (newVal < 0) Interlocked.Exchange(ref _activeUsersCounter, 0);
        _metrics.UpdateActiveUsers(Math.Max(newVal, 0));
    }
}