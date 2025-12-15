namespace BankingSystem.Application.Commands.Transactions.Handlers;

using BankingSystem.Application.Commands.Transactions;
using BankingSystem.Application.Constants;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Models;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Enums;
using BankingSystem.Domain.Exceptions;
using BankingSystem.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
        var account = await _context.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);
        
        if (account == null)
            throw new NotFoundException(string.Format(ValidationMessages.AccountNotFound, request.AccountId));

        var amount = new Money(request.Amount, "USD");
        var transactionType = Enum.Parse<TransactionType>(request.TransactionType);

        // Calculate balance before transaction
        var balanceBefore = account.Balance;

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

        // Create Outbox Message for async notification/email
        var notificationData = new
        {
            UserId = account.UserId,
            UserEmail = account.User?.Email,
            UserFirstName = account.User?.FirstName,
            UserLastName = account.User?.LastName,
            TransactionId = transaction.Id,
            TransactionType = transactionType.ToString(),
            Amount = amount.Amount,
            Currency = amount.Currency,
            AccountNumber = account.AccountNumber,
            BalanceBefore = balanceBefore.Amount,
            BalanceAfter = account.Balance.Amount,
            Description = request.Description,
            TransactionReference = transaction.ReferenceNumber,
            TransactionDate = transaction.TransactionDate
        };

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "TransactionCompletedEvent",
            EventData = JsonSerializer.Serialize(notificationData),
            CreatedAt = DateTime.UtcNow,
            IsProcessed = false
        };

        _context.OutboxMessages.Add(outboxMessage);

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
