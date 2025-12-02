namespace BankingSystem.Infrastructure.BackgroundJobs;

using BankingSystem.Application.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// Background job for applying interest to all eligible accounts
/// Runs monthly (or configurable schedule)
/// </summary>
public class InterestApplicationJob
{
    private readonly IInterestCalculationService _interestService;
    private readonly ILogger<InterestApplicationJob> _logger;

    public InterestApplicationJob(
        IInterestCalculationService interestService,
        ILogger<InterestApplicationJob> logger)
    {
        _interestService = interestService;
        _logger = logger;
    }

    /// <summary>
    /// Apply interest to all eligible savings accounts
    /// </summary>
    public async Task ApplyMonthlyInterest()
    {
        _logger.LogInformation("Starting monthly interest application job");

        try
        {
            var count = await _interestService.ApplyInterestToAllAccountsAsync(CancellationToken.None);

            _logger.LogInformation(
                "Monthly interest application completed successfully. {Count} accounts processed.",
                count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying monthly interest");
            throw; // Hangfire will retry
        }
    }
}
