namespace BankingSystem.Application.Interfaces;

using BankingSystem.Domain.Entities;

/// <summary>
/// Service for calculating and applying interest to accounts
/// </summary>
public interface IInterestCalculationService
{
    /// <summary>
    /// Calculate interest for a specific account based on current balance and time period
    /// </summary>
    /// <param name="account">The account to calculate interest for</param>
    /// <param name="days">Number of days to calculate interest for</param>
    /// <returns>The calculated interest amount</returns>
    decimal CalculateInterest(Account account, int days);

    /// <summary>
    /// Apply interest to a specific account
    /// </summary>
    Task<bool> ApplyInterestAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply interest to all eligible accounts (typically run monthly)
    /// </summary>
    Task<int> ApplyInterestToAllAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the annual interest rate for an account type
    /// </summary>
    decimal GetAnnualRate(Domain.Enums.AccountType accountType);
}
