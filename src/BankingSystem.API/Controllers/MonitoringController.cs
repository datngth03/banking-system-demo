using BankingSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystem.API.Controllers;

/// <summary>
/// Monitoring and diagnostics endpoints (Admin only)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
[Produces("application/json")]
public class MonitoringController : ControllerBase
{
    private readonly IErrorTrackingService _errorTracker;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(
        IErrorTrackingService errorTracker,
        ILogger<MonitoringController> logger)
    {
        _errorTracker = errorTracker;
        _logger = logger;
    }

    /// <summary>
    /// Get error statistics for the last hour
    /// </summary>
    [HttpGet("errors/last-hour")]
    [ProducesResponseType(typeof(ErrorStatistics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetErrorStatisticsLastHour()
    {
        var stats = await _errorTracker.GetErrorStatisticsAsync(TimeSpan.FromHours(1));
        return Ok(stats);
    }

    /// <summary>
    /// Get error statistics for the last 24 hours
    /// </summary>
    [HttpGet("errors/last-24hours")]
    [ProducesResponseType(typeof(ErrorStatistics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetErrorStatisticsLast24Hours()
    {
        var stats = await _errorTracker.GetErrorStatisticsAsync(TimeSpan.FromHours(24));
        return Ok(stats);
    }

    /// <summary>
    /// Get error statistics for custom time range
    /// </summary>
    [HttpGet("errors/custom")]
    [ProducesResponseType(typeof(ErrorStatistics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetErrorStatisticsCustom([FromQuery] int hours = 1)
    {
        if (hours < 1 || hours > 168) // Max 7 days
        {
            return BadRequest(new { error = "Hours must be between 1 and 168 (7 days)" });
        }

        var stats = await _errorTracker.GetErrorStatisticsAsync(TimeSpan.FromHours(hours));
        return Ok(stats);
    }

    /// <summary>
    /// Health check for monitoring system
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult MonitoringHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "MonitoringController"
        });
    }
}
