namespace BankingSystem.Application.Interfaces;

public interface ITransactionService
{
    Task<dynamic?> GetTransactionByIdAsync(Guid transactionId);
    Task<IEnumerable<dynamic>> GetAccountTransactionsAsync(Guid accountId, int pageNumber = 1, int pageSize = 20);
    Task<IEnumerable<dynamic>> GetUserTransactionsAsync(Guid userId, int pageNumber = 1, int pageSize = 20);
}
