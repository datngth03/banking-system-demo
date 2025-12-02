namespace BankingSystem.Application.Commands.Accounts.Handlers;

using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Commands.Accounts;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Enums;
using BankingSystem.Domain.ValueObjects;
using BankingSystem.Domain.Exceptions;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class TransferFundsHandler : IRequestHandler<TransferFundsCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<TransferFundsHandler> _logger;

    public TransferFundsHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<TransferFundsHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Unit> Handle(TransferFundsCommand request, CancellationToken cancellationToken)
    {
        if (request.FromAccountId == request.ToAccountId)
            throw new BankingApplicationException(ValidationMessages.CannotTransferToSameAccount);

        var fromAccount = await _context.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == request.FromAccountId, cancellationToken);
            
        if (fromAccount == null)
            throw new NotFoundException(string.Format(ValidationMessages.AccountNotFound, request.FromAccountId));

        var toAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.ToAccountId, cancellationToken);
            
        if (toAccount == null)
            throw new NotFoundException(string.Format(ValidationMessages.AccountNotFound, request.ToAccountId));

        // Authorization: Users can only transfer from their own accounts, staff can transfer from any
        if (!_currentUserService.IsStaff && fromAccount.UserId != _currentUserService.UserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to transfer from account {AccountId} owned by {OwnerId}",
                _currentUserService.UserId,
                fromAccount.Id,
                fromAccount.UserId);
                
            throw new ForbiddenException("You can only transfer from your own accounts");
        }

        var transferAmount = new Money(request.Amount, "USD");

        if (fromAccount.Balance.IsLessThan(transferAmount))
            throw new InsufficientFundsException(ValidationMessages.InsufficientFunds);

        // Debit from account
        var newFromBalance = fromAccount.Balance - transferAmount;
        fromAccount.Balance = newFromBalance;

        // Create debit transaction
        var debitTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = fromAccount.Id,
            RelatedAccountId = toAccount.Id,
            TransactionType = TransactionType.Transfer,
            Amount = transferAmount,
            BalanceAfter = newFromBalance,
            Description = $"Transfer to {toAccount.AccountNumber}: {request.Description}",
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ReferenceNumber = GenerateReferenceNumber()
        };

        // Credit to account
        var newToBalance = toAccount.Balance + transferAmount;
        toAccount.Balance = newToBalance;

        // Create credit transaction
        var creditTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = toAccount.Id,
            RelatedAccountId = fromAccount.Id,
            TransactionType = TransactionType.Transfer,
            Amount = transferAmount,
            BalanceAfter = newToBalance,
            Description = $"Transfer from {fromAccount.AccountNumber}: {request.Description}",
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ReferenceNumber = debitTransaction.ReferenceNumber
        };

        _context.Transactions.Add(debitTransaction);
        _context.Transactions.Add(creditTransaction);

        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation(
            "Transfer completed: {Amount} from account {FromAccount} to {ToAccount} by user {UserId}",
            transferAmount.Amount,
            fromAccount.AccountNumber,
            toAccount.AccountNumber,
            _currentUserService.UserId);
            
        return Unit.Value;
    }

    private static string GenerateReferenceNumber()
    {
        return $"TRF{DateTime.UtcNow.Ticks}{Random.Shared.Next(1000, 9999)}";
    }
}
