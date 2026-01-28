namespace BankingSystem.Infrastructure.BackgroundJobs;

using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Enums;
using BankingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Background job for applying interest to all eligible accounts
/// Runs monthly (or configurable schedule)
/// </summary>
public class InterestApplicationJob
{
    private readonly BankingSystemDbContext _context;
    private readonly ILogger<InterestApplicationJob> _logger;

    // Interest rates per account type (annual percentage rate)
    private static readonly Dictionary<AccountType, decimal> InterestRates = new()
    {
        { AccountType.Savings, 0.025m },           // 2.5% per year
        { AccountType.MoneyMarket, 0.035m },       // 3.5% per year
        { AccountType.CertificateOfDeposit, 0.04m }, // 4.0% per year
        { AccountType.Checking, 0m }               // 0% for checking accounts
    };

    public InterestApplicationJob(
        BankingSystemDbContext context,
        ILogger<InterestApplicationJob> logger)
    {
        _context = context;
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
            // Get all active accounts that are eligible for interest
            var eligibleAccounts = await _context.Accounts
                .Where(a => a.IsActive && 
                           (a.AccountType == AccountType.Savings || 
                            a.AccountType == AccountType.MoneyMarket || 
                            a.AccountType == AccountType.CertificateOfDeposit))
                .ToListAsync();

            _logger.LogInformation("Found {Count} eligible accounts for interest calculation", eligibleAccounts.Count);

            var interestAppliedCount = 0;
            var totalInterestApplied = 0m;

            foreach (var account in eligibleAccounts)
            {
                try
                {
                    // Calculate monthly interest
                    var annualRate = InterestRates[account.AccountType];
                    var monthlyRate = annualRate / 12;
                    var interestAmount = account.Balance.Amount * monthlyRate;

                    // Only apply if interest is greater than 0.01 (1 cent)
                    if (interestAmount > 0.01m)
                    {
                        // Update account balance
                        var newBalance = account.Balance.Amount + interestAmount;
                        account.Balance = new(newBalance, account.Balance.Currency);

                        // Create a transaction record for the interest
                        var interestTransaction = new Transaction
                        {
                            Id = Guid.NewGuid(),
                            AccountId = account.Id,
                            TransactionType = TransactionType.Deposit,
                            Amount = new(interestAmount, account.Balance.Currency),
                            BalanceAfter = account.Balance,
                            Description = $"Monthly interest applied - {account.AccountType} account",
                            ReferenceNumber = $"INT-{account.Id:N}-{DateTime.UtcNow:yyyyMM}",
                            TransactionDate = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Transactions.Add(interestTransaction);
                        interestAppliedCount++;
                        totalInterestApplied += interestAmount;

                        _logger.LogDebug(
                            "Applied interest of {Interest} to account {AccountId} ({AccountNumber})",
                            interestAmount.ToString("C"),
                            account.Id,
                            account.AccountNumber);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error applying interest to account {AccountId}",
                        account.Id);
                    // Continue with next account instead of failing entire job
                }
            }

            // Save all changes
            if (interestAppliedCount > 0)
            {
                var saveResult = await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Monthly interest application job completed successfully. " +
                    "Applied interest to {Count} accounts, Total interest: {Total}, Changes saved: {Saved}",
                    interestAppliedCount,
                    totalInterestApplied.ToString("C"),
                    saveResult);
            }
            else
            {
                _logger.LogInformation("No accounts eligible for interest application this month");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during monthly interest application");
            throw;
        }

        await Task.CompletedTask;
    }
}
