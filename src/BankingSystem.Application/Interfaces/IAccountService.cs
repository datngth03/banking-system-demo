namespace BankingSystem.Application.Interfaces;

using BankingSystem.Application.DTOs.Accounts;

/// <summary>
/// Service for account-related operations
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Checks if an account exists by ID
    /// </summary>
    Task<bool> AccountExistsAsync(Guid accountId);

    /// <summary>
    /// Checks if an account number already exists
    /// </summary>
    Task<bool> AccountNumberExistsAsync(string accountNumber);

    /// <summary>
    /// Gets account details by ID
    /// </summary>
    Task<AccountDto?> GetAccountByIdAsync(Guid accountId);

    /// <summary>
    /// Gets all accounts for a user
    /// </summary>
    Task<IEnumerable<AccountDto>> GetUserAccountsAsync(Guid userId);

    /// <summary>
    /// Gets account balance
    /// </summary>
    Task<decimal> GetAccountBalanceAsync(Guid accountId);

    /// <summary>
    /// Gets available credit for an account
    /// </summary>
    Task<decimal> GetAvailableCreditAsync(Guid accountId);
}
