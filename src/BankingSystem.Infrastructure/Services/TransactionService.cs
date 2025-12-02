namespace BankingSystem.Infrastructure.Services;

using BankingSystem.Application.Interfaces;
using BankingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class TransactionService : ITransactionService
{
    private readonly BankingSystemDbContext _context;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        BankingSystemDbContext context,
        ILogger<TransactionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<dynamic?> GetTransactionByIdAsync(Guid transactionId)
    {
        var transaction = await _context.Transactions
            .Where(t => t.Id == transactionId)
            .Select(t => new
            {
                t.Id,
                t.AccountId,
                t.RelatedAccountId,
                TransactionType = t.TransactionType.ToString(),
                Amount = t.Amount.Amount,
                Currency = t.Amount.Currency,
                BalanceAfter = t.BalanceAfter.Amount,
                t.Description,
                t.ReferenceNumber,
                t.TransactionDate,
                t.CreatedAt
            })
            .FirstOrDefaultAsync();

        return transaction;
    }

    public async Task<IEnumerable<dynamic>> GetAccountTransactionsAsync(Guid accountId, int pageNumber = 1, int pageSize = 20)
    {
        var transactions = await _context.Transactions
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.TransactionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.AccountId,
                t.RelatedAccountId,
                TransactionType = t.TransactionType.ToString(),
                Amount = t.Amount.Amount,
                Currency = t.Amount.Currency,
                BalanceAfter = t.BalanceAfter.Amount,
                t.Description,
                t.ReferenceNumber,
                t.TransactionDate,
                t.CreatedAt
            })
            .ToListAsync();

        return transactions;
    }

    public async Task<IEnumerable<dynamic>> GetUserTransactionsAsync(Guid userId, int pageNumber = 1, int pageSize = 20)
    {
        var transactions = await _context.Transactions
            .Include(t => t.Account) // Ensure join for UserId filter
            .Where(t => t.Account!.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.AccountId,
                t.RelatedAccountId,
                TransactionType = t.TransactionType.ToString(),
                Amount = t.Amount.Amount,
                Currency = t.Amount.Currency,
                BalanceAfter = t.BalanceAfter.Amount,
                t.Description,
                t.ReferenceNumber,
                t.TransactionDate,
                t.CreatedAt
            })
            .ToListAsync();

        return transactions;
    }
}
