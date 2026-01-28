namespace BankingSystem.Infrastructure.Services;

using BankingSystem.Application.DTOs.Accounts;
using BankingSystem.Application.Interfaces;
using BankingSystem.Infrastructure.Caching;
using BankingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of IAccountService
/// </summary>
public class AccountService : IAccountService
{
    private readonly BankingSystemDbContext _context;
    private readonly ILogger<AccountService> _logger;
    private readonly ICacheService _cacheService;

    public AccountService(
        BankingSystemDbContext context,
        ILogger<AccountService> logger,
        ICacheService cacheService)
    {
        _context = context;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<bool> AccountExistsAsync(Guid accountId)
    {
        return await _context.Accounts.AnyAsync(a => a.Id == accountId);
    }

    public async Task<bool> AccountNumberExistsAsync(string accountNumber)
    {
        return await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber);
    }

    public async Task<AccountDto?> GetAccountByIdAsync(Guid accountId)
    {
        var cacheKey = CacheKeys.GetAccountKey(accountId);

        // Try cache first
        var cachedAccount = await _cacheService.GetAsync<AccountDto>(cacheKey);
        if (cachedAccount != null)
        {
            _logger.LogInformation("Account {AccountId} retrieved from cache", accountId);
            return cachedAccount;
        }

        var account = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Id == accountId)
            .Select(a => new AccountDto
            {
                Id = a.Id,
                AccountNumber = a.AccountNumber,
                AccountType = a.AccountType.ToString(),
                Balance = a.Balance.Amount,
                Currency = a.Balance.Currency,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                IBAN = a.IBAN,
                BIC = a.BIC,
                UserId = a.UserId
            })
            .FirstOrDefaultAsync();

        if (account != null)
        {
            await _cacheService.SetAsync(cacheKey, account, TimeSpan.FromHours(1));
        }
        else
        {
            _logger.LogDebug("Account {AccountId} not found", accountId);
        }

        return account;
    }

    public async Task<IEnumerable<AccountDto>> GetUserAccountsAsync(Guid userId)
    {
        var cacheKey = CacheKeys.GetUserAccountsKey(userId);

        // Try cache first
        var cachedAccounts = await _cacheService.GetAsync<List<AccountDto>>(cacheKey);
        if (cachedAccounts != null && cachedAccounts.Any())
        {
            _logger.LogInformation("User {UserId} accounts retrieved from cache", userId);
            return cachedAccounts;
        }

        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.IsActive)
            .Select(a => new AccountDto
            {
                Id = a.Id,
                AccountNumber = a.AccountNumber,
                AccountType = a.AccountType.ToString(),
                Balance = a.Balance.Amount,
                Currency = a.Balance.Currency,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                IBAN = a.IBAN,
                BIC = a.BIC,
                UserId = a.UserId
            })
            .ToListAsync();

        // Cache the results
        if (accounts.Any())
        {
            await _cacheService.SetAsync(cacheKey, accounts, TimeSpan.FromHours(1));
        }

        _logger.LogDebug("Retrieved {Count} accounts for user {UserId}", accounts.Count, userId);
        return accounts;
    }

    public async Task<decimal> GetAccountBalanceAsync(Guid accountId)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account == null)
        {
            _logger.LogWarning("Account {AccountId} not found", accountId);
            return 0;
        }

        return account.Balance.Amount;
    }

    public async Task<decimal> GetAvailableCreditAsync(Guid accountId)
    {
        // TODO: Implement based on account type and credit limits
        var balance = await GetAccountBalanceAsync(accountId);
        return balance;
    }
}
