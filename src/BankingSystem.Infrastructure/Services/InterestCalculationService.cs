namespace BankingSystem.Infrastructure.Services;

using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Models;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Enums;
using BankingSystem.Domain.ValueObjects;
using BankingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class InterestCalculationService : IInterestCalculationService
{
    private readonly BankingSystemDbContext _context;
    private readonly InterestSettings _settings;
    private readonly ILogger<InterestCalculationService> _logger;

    public InterestCalculationService(
        BankingSystemDbContext context,
        IOptions<InterestSettings> settings,
        ILogger<InterestCalculationService> logger)
    {
        _context = context;
        _settings = settings.Value;
        _logger = logger;
    }

    public decimal CalculateInterest(Account account, int days)
    {
        // Only calculate interest for eligible account types
        if (account.AccountType == AccountType.Checking)
        {
            return 0m; // Checking accounts typically don't earn interest
        }

        // Check minimum balance requirement
        if (account.Balance.Amount < _settings.MinimumBalanceForInterest)
        {
            return 0m;
        }

        var annualRate = GetAnnualRate(account.AccountType);
        if (annualRate == 0m)
        {
            return 0m;
        }

        // Calculate daily interest rate
        var dailyRate = annualRate / 365m;

        // Simple interest formula: Principal × Rate × Time
        var interest = account.Balance.Amount * dailyRate * days;

        _logger.LogDebug(
            "Calculated interest for account {AccountId}: {Interest} ({Days} days at {Rate}% annual)",
            account.Id,
            interest,
            days,
            annualRate * 100);

        return Math.Round(interest, 2);
    }

    public async Task<bool> ApplyInterestAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive, cancellationToken);

        if (account == null)
        {
            _logger.LogWarning("Account {AccountId} not found or inactive", accountId);
            return false;
        }

        // Calculate interest for the period (typically monthly = 30 days)
        var periodDays = _settings.CompoundingFrequency switch
        {
            CompoundingFrequency.Daily => 1,
            CompoundingFrequency.Monthly => 30,
            CompoundingFrequency.Quarterly => 90,
            CompoundingFrequency.Annually => 365,
            _ => 30
        };

        var interest = CalculateInterest(account, periodDays);

        if (interest <= 0)
        {
            _logger.LogInformation(
                "No interest to apply for account {AccountId} (Balance: {Balance}, Type: {Type})",
                accountId,
                account.Balance.Amount,
                account.AccountType);
            return false;
        }

        // Apply interest to account
        var interestMoney = new Money(interest, account.Balance.Currency);
        var newBalance = account.Balance + interestMoney;
        account.Balance = newBalance;

        // Create transaction record
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            TransactionType = TransactionType.InterestCredit,
            Amount = interestMoney,
            BalanceAfter = newBalance,
            Description = $"Interest credit for {_settings.CompoundingFrequency} period",
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ReferenceNumber = $"INT{DateTime.UtcNow.Ticks}{Random.Shared.Next(1000, 9999)}"
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Applied interest of {Interest} to account {AccountId}, new balance: {Balance}",
            interest,
            accountId,
            newBalance.Amount);

        return true;
    }

    public async Task<int> ApplyInterestToAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting interest application for all eligible accounts");

        var eligibleAccounts = await _context.Accounts
            .Where(a => a.IsActive &&
                       (a.AccountType == AccountType.Savings ||
                        a.AccountType == AccountType.MoneyMarket ||
                        a.AccountType == AccountType.CertificateOfDeposit))
            .ToListAsync(cancellationToken);

        int successCount = 0;

        foreach (var account in eligibleAccounts)
        {
            try
            {
                var applied = await ApplyInterestAsync(account.Id, cancellationToken);
                if (applied)
                {
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error applying interest to account {AccountId}",
                    account.Id);
            }
        }

        _logger.LogInformation(
            "Completed interest application: {SuccessCount}/{TotalCount} accounts processed",
            successCount,
            eligibleAccounts.Count);

        return successCount;
    }

    public decimal GetAnnualRate(AccountType accountType)
    {
        return accountType switch
        {
            AccountType.Savings => _settings.SavingsAnnualRate,
            AccountType.Checking => _settings.CheckingAnnualRate,
            AccountType.MoneyMarket => _settings.InvestmentAnnualRate,
            AccountType.CertificateOfDeposit => _settings.InvestmentAnnualRate,
            _ => 0m
        };
    }
}
