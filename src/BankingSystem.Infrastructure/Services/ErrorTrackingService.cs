using BankingSystem.Application.Interfaces;
using BankingSystem.Infrastructure.Monitoring;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BankingSystem.Infrastructure.Services;

/// <summary>
/// Error tracking service with categorization and statistics
/// </summary>
public class ErrorTrackingService : IErrorTrackingService
{
    private readonly ILogger<ErrorTrackingService> _logger;
    private readonly BankingSystemMetrics _metrics;
    
    // In-memory storage for recent errors (for statistics)
    // In production, this should be stored in database or external service
    private static readonly ConcurrentQueue<ErrorRecord> _recentErrors = new();
    private const int MaxRecentErrors = 1000;

    public ErrorTrackingService(
        ILogger<ErrorTrackingService> logger,
        BankingSystemMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public async Task TrackErrorAsync(
        Exception exception, 
        Dictionary<string, object>? context = null, 
        string? userId = null)
    {
        var errorRecord = new ErrorRecord
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            ExceptionType = exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            InnerException = exception.InnerException?.Message,
            UserId = userId,
            Context = context ?? new Dictionary<string, object>(),
            Severity = DetermineErrorSeverity(exception)
        };

        // Store in memory (limited to MaxRecentErrors)
        _recentErrors.Enqueue(errorRecord);
        while (_recentErrors.Count > MaxRecentErrors)
        {
            _recentErrors.TryDequeue(out _);
        }

        // Record metrics (use the ErrorSeverity from Monitoring namespace)
        _metrics.RecordError(errorRecord.ExceptionType, errorRecord.Severity);

        // Log with structured data
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["ErrorId"] = errorRecord.Id,
            ["UserId"] = userId ?? "Anonymous",
            ["ExceptionType"] = errorRecord.ExceptionType,
            ["Severity"] = errorRecord.Severity,
            ["Context"] = context ?? new Dictionary<string, object>()
        }))
        {
            var logLevel = errorRecord.Severity switch
            {
                ErrorSeverity.Critical => LogLevel.Critical,
                ErrorSeverity.High => LogLevel.Error,
                ErrorSeverity.Medium => LogLevel.Warning,
                _ => LogLevel.Information
            };

            _logger.Log(logLevel, exception, 
                "Error tracked: {ExceptionType} - {Message}", 
                errorRecord.ExceptionType, 
                errorRecord.Message);
        }

        await Task.CompletedTask;
    }

    public async Task TrackWarningAsync(string message, Dictionary<string, object>? context = null)
    {
        var errorRecord = new ErrorRecord
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            ExceptionType = "Warning",
            Message = message,
            Context = context ?? new Dictionary<string, object>(),
            Severity = ErrorSeverity.Low
        };

        _recentErrors.Enqueue(errorRecord);
        while (_recentErrors.Count > MaxRecentErrors)
        {
            _recentErrors.TryDequeue(out _);
        }

        _logger.LogWarning("Warning tracked: {Message}. Context: {@Context}", message, context);

        await Task.CompletedTask;
    }

    public async Task<ErrorStatistics> GetErrorStatisticsAsync(TimeSpan timeRange)
    {
        var cutoffTime = DateTime.UtcNow - timeRange;
        var recentErrors = _recentErrors.Where(e => e.Timestamp >= cutoffTime).ToList();

        var statistics = new ErrorStatistics
        {
            FromDate = cutoffTime,
            ToDate = DateTime.UtcNow,
            TotalErrors = recentErrors.Count,
            CriticalErrors = recentErrors.Count(e => e.Severity == ErrorSeverity.Critical),
            ErrorsByType = recentErrors
                .GroupBy(e => e.ExceptionType)
                .ToDictionary(g => g.Key, g => g.Count()),
            ErrorsByEndpoint = recentErrors
                .Where(e => e.Context.ContainsKey("Endpoint"))
                .GroupBy(e => e.Context["Endpoint"].ToString() ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return await Task.FromResult(statistics);
    }

    private static ErrorSeverity DetermineErrorSeverity(Exception exception)
    {
        return exception switch
        {
            // Critical - data integrity or system failure
            InvalidOperationException => ErrorSeverity.Critical,
            NullReferenceException => ErrorSeverity.Critical,
            
            // High - business logic failures
            UnauthorizedAccessException => ErrorSeverity.High,
            ArgumentException => ErrorSeverity.High,
            
            // Medium - expected failures
            TimeoutException => ErrorSeverity.Medium,
            
            // Default
            _ => ErrorSeverity.Medium
        };
    }
}

public class ErrorRecord
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? InnerException { get; set; }
    public string? UserId { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public ErrorSeverity Severity { get; set; }
}
