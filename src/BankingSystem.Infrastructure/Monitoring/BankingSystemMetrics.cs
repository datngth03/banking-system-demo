namespace BankingSystem.Infrastructure.Monitoring;

using System.Diagnostics.Metrics;

/// <summary>
/// Custom metrics for banking system business KPIs (low-level meter wrapper)
/// </summary>
public class BankingSystemMetrics
{
    private readonly Meter _meter;

    // Counters/histograms/gauges
    private readonly Counter<long> _transactionCounter;
    private readonly Counter<long> _loginAttemptCounter;
    private readonly Counter<long> _failedLoginCounter;
    private readonly Counter<long> _cardIssuedCounter;
    private readonly Counter<long> _accountCreatedCounter;
    private readonly Counter<long> _errorCounter;
    private readonly Counter<long> _cacheHitCounter;
    private readonly Counter<long> _cacheMissCounter;

    private readonly Histogram<double> _transactionAmountHistogram;
    private readonly Histogram<double> _apiDurationHistogram;
    private readonly Histogram<double> _dbDurationHistogram;

    // Observable gauges (updated via Update* methods)
    private long _activeUsersCount;
    private long _totalAccountsCount;
    private long _pendingTransactionsCount;

    // Parameterless ctor for tests and a ctor accepting IMeterFactory for DI scenarios
    public BankingSystemMetrics()
        : this(null)
    {
    }

    public BankingSystemMetrics(IMeterFactory? meterFactory)
    {
        _meter = meterFactory?.Create("BankingSystem.Metrics") ?? new Meter("BankingSystem.Metrics", "1.0.0");

        _transactionCounter = _meter.CreateCounter<long>("banking.transactions.count", description: "Total number of transactions processed");
        _loginAttemptCounter = _meter.CreateCounter<long>("banking.login.attempts", description: "Total login attempts");
        _failedLoginCounter = _meter.CreateCounter<long>("banking.login.failed", description: "Failed login attempts");
        _cardIssuedCounter = _meter.CreateCounter<long>("banking.cards.issued", description: "Total cards issued");
        _accountCreatedCounter = _meter.CreateCounter<long>("banking.accounts.created", description: "Total accounts created");
        _errorCounter = _meter.CreateCounter<long>("banking.errors.count", description: "Total errors occurred");
        _cacheHitCounter = _meter.CreateCounter<long>("banking.cache.hits", description: "Cache hits");
        _cacheMissCounter = _meter.CreateCounter<long>("banking.cache.misses", description: "Cache misses");

        _transactionAmountHistogram = _meter.CreateHistogram<double>("banking.transaction.amount", unit: "USD", description: "Transaction amounts distribution");
        _apiDurationHistogram = _meter.CreateHistogram<double>("banking.api.duration.ms", unit: "ms", description: "API request duration (ms)");
        _dbDurationHistogram = _meter.CreateHistogram<double>("banking.database.query_duration.ms", unit: "ms", description: "DB query duration (ms)");

        // Register observable gauges
        _meter.CreateObservableGauge("banking.users.active", () => _activeUsersCount, unit: "users", description: "Number of active users");
        _meter.CreateObservableGauge("banking.accounts.total", () => _totalAccountsCount, unit: "accounts", description: "Total number of accounts");
        _meter.CreateObservableGauge("banking.transactions.pending", () => _pendingTransactionsCount, unit: "transactions", description: "Pending transactions");
    }

    // Transaction metrics
    public void RecordTransaction(string transactionType, double amount, bool success)
    {
        _transactionCounter.Add(1, new KeyValuePair<string, object?>("type", transactionType), new KeyValuePair<string, object?>("success", success));
        if (success)
            _transactionAmountHistogram.Record(amount, new KeyValuePair<string, object?>("type", transactionType));
    }

    // Login metrics
    public void RecordLoginAttempt(bool success)
    {
        _loginAttemptCounter.Add(1);
        if (!success) _failedLoginCounter.Add(1);
    }

    // Card metrics
    public void RecordCardIssued(string cardType)
    {
        _cardIssuedCounter.Add(1, new KeyValuePair<string, object?>("type", cardType));
    }

    // Account metrics
    public void RecordAccountCreated(string accountType, double initialBalance)
    {
        _accountCreatedCounter.Add(1, new KeyValuePair<string, object?>("type", accountType));
        // Optionally record balance distribution
        _transactionAmountHistogram.Record(initialBalance, new KeyValuePair<string, object?>("type", accountType));
    }

    // Error tracking metrics
    public void RecordError(string errorType, ErrorSeverity severity)
    {
        _errorCounter.Add(1, 
            new KeyValuePair<string, object?>("error_type", errorType),
            new KeyValuePair<string, object?>("severity", severity.ToString()));
    }

    // Cache metrics
    public void RecordCacheHit(string cacheType)
    {
        _cacheHitCounter.Add(1, new KeyValuePair<string, object?>("cache_type", cacheType));
    }

    public void RecordCacheMiss(string cacheType)
    {
        _cacheMissCounter.Add(1, new KeyValuePair<string, object?>("cache_type", cacheType));
    }

    // API / DB durations
    public void RecordApiDuration(string endpoint, double durationMs) => _apiDurationHistogram.Record(durationMs, new KeyValuePair<string, object?>("endpoint", endpoint));
    public void RecordDatabaseQueryDuration(string queryType, double durationMs) => _dbDurationHistogram.Record(durationMs, new KeyValuePair<string, object?>("query_type", queryType));

    // Observable gauges update
    public void UpdateActiveUsers(long count) => _activeUsersCount = count;
    public void UpdateTotalAccounts(long count) => _totalAccountsCount = count;
    public void UpdatePendingTransactions(long count) => _pendingTransactionsCount = count;
}

public enum ErrorSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}