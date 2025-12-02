namespace BankingSystem.Application.Interfaces;

/// <summary>
/// Service for recording business metrics and application telemetry
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Records a transaction execution
    /// </summary>
    void RecordTransaction(string transactionType, decimal amount, bool isSuccess);

    /// <summary>
    /// Records an account operation
    /// </summary>
    void RecordAccountOperation(string operationType, bool isSuccess);

    /// <summary>
    /// Records user authentication
    /// </summary>
    void RecordAuthentication(bool isSuccess, string? failureReason = null);

    /// <summary>
    /// Records API endpoint duration
    /// </summary>
    void RecordApiDuration(string endpoint, double durationMs);

    /// <summary>
    /// Records database query duration
    /// </summary>
    void RecordDatabaseQueryDuration(string queryType, double durationMs);

    /// <summary>
    /// Increments active user count
    /// </summary>
    void IncrementActiveUsers();

    /// <summary>
    /// Decrements active user count
    /// </summary>
    void DecrementActiveUsers();
}