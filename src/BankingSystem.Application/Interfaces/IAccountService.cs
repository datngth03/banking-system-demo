namespace BankingSystem.Application.Interfaces;

public interface IAccountService
{
    Task<bool> AccountExistsAsync(Guid accountId);
    Task<bool> AccountNumberExistsAsync(string accountNumber);
    Task<dynamic?> GetAccountByIdAsync(Guid accountId);
    Task<IEnumerable<dynamic>> GetUserAccountsAsync(Guid userId);
    Task<decimal> GetAccountBalanceAsync(Guid accountId);
}
