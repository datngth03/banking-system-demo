namespace BankingSystem.Application.Interfaces;

/// <summary>
/// Service for tracking and categorizing errors
/// </summary>
public interface IErrorTrackingService
{
    Task TrackErrorAsync(Exception exception, Dictionary<string, object>? context = null, string? userId = null);
    Task TrackWarningAsync(string message, Dictionary<string, object>? context = null);
    Task<ErrorStatistics> GetErrorStatisticsAsync(TimeSpan timeRange);
}

public class ErrorStatistics
{
    public int TotalErrors { get; set; }
    public int CriticalErrors { get; set; }
    public Dictionary<string, int> ErrorsByType { get; set; } = new();
    public Dictionary<string, int> ErrorsByEndpoint { get; set; } = new();
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}
