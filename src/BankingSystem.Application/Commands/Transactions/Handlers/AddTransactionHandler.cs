namespace BankingSystem.Application.Commands.Transactions.Handlers;

using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Commands.Transactions;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Enums;
using BankingSystem.Domain.ValueObjects;
using BankingSystem.Application.Exceptions;
using BankingSystem.Domain.Exceptions;
using BankingSystem.Application.Constants;
using Microsoft.EntityFrameworkCore;

public class AddTransactionHandler : IRequestHandler<AddTransactionCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;

    public AddTransactionHandler(
        IApplicationDbContext context,
        ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<Guid> Handle(AddTransactionCommand request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);
        if (account == null)
            throw new NotFoundException(string.Format(ValidationMessages.AccountNotFound, request.AccountId));

        var amount = new Money(request.Amount, "USD");
        var transactionType = Enum.Parse<TransactionType>(request.TransactionType);

        // Update account balance based on transaction type
        if (transactionType == TransactionType.Deposit || transactionType == TransactionType.InterestCredit || transactionType == TransactionType.Refund)
        {
            account.Balance = account.Balance + amount;
        }
        else if (transactionType == TransactionType.Withdrawal || transactionType == TransactionType.Fee)
        {
            if (account.Balance.IsLessThan(amount))
                throw new InsufficientFundsException(ValidationMessages.InsufficientFunds);
            account.Balance = account.Balance - amount;
        }

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId,
            TransactionType = transactionType,
            Amount = amount,
            BalanceAfter = account.Balance,
            Description = request.Description,
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ReferenceNumber = GenerateReferenceNumber()
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate account cache since balance changed
        await _cacheService.RemoveAsync($"account:{request.AccountId}", cancellationToken);
        await _cacheService.RemoveAsync($"account:user:{account.UserId}", cancellationToken);

        return transaction.Id;
    }

    private static string GenerateReferenceNumber()
    {
        return $"TXN{DateTime.UtcNow.Ticks}{Random.Shared.Next(1000, 9999)}";
    }
}
