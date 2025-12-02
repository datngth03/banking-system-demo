namespace BankingSystem.Infrastructure.Services;

using BankingSystem.Application.Interfaces;
using BankingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

public class AccountService : IAccountService
{
    private readonly BankingSystemDbContext _context;
    private readonly ILogger<AccountService> _logger;
    private readonly IDistributedCache _cache;

    public AccountService(
        BankingSystemDbContext context,
        ILogger<AccountService> logger,
        IDistributedCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
    }

    public async Task<bool> AccountExistsAsync(Guid accountId)
    {
        return await _context.Accounts.AnyAsync(a => a.Id == accountId);
    }

    public async Task<bool> AccountNumberExistsAsync(string accountNumber)
    {
        return await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber);
    }

    public async Task<dynamic?> GetAccountByIdAsync(Guid accountId)
    {
        var cacheKey = $"account:{accountId}";

        // Try cache first
        var cachedAccount = await _cache.GetStringAsync(cacheKey);
        if (cachedAccount != null)
        {
            _logger.LogInformation("Account {AccountId} retrieved from cache", accountId);
            return JsonSerializer.Deserialize<dynamic>(cachedAccount);
        }

        var account = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Id == accountId)
            .Select(a => new
            {
                a.Id,
                a.AccountNumber,
                a.AccountType,
                Balance = a.Balance.Amount,
                Currency = a.Balance.Currency,
                a.IsActive,
                a.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (account != null)
        {
            // Cache for 5 minutes
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(account), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
            _logger.LogInformation("Account {AccountId} cached", accountId);
        }

        return account;
    }

    public async Task<IEnumerable<dynamic>> GetUserAccountsAsync(Guid userId)
    {
        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new
            {
                a.Id,
                a.AccountNumber,
                a.AccountType,
                Balance = a.Balance.Amount,
                Currency = a.Balance.Currency,
                a.IsActive,
                a.CreatedAt
            })
            .ToListAsync();

        return accounts;
    }

    public async Task<decimal> GetAccountBalanceAsync(Guid accountId)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Id == accountId)
            .FirstOrDefaultAsync();

        if (account is null)
        {
            _logger.LogWarning("Account with ID {AccountId} not found", accountId);
            return 0;
        }

        return account.Balance.Amount;
    }
}
