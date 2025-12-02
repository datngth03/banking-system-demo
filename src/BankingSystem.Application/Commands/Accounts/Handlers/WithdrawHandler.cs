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

namespace BankingSystem.Application.Commands.Accounts.Handlers;

public class WithdrawHandler : IRequestHandler<WithdrawCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<WithdrawHandler> _logger;

    public WithdrawHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<WithdrawHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Unit> Handle(WithdrawCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new ValidationFailureException(ValidationMessages.InvalidAmount);

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

        if (account == null)
            throw new NotFoundException(string.Format(ValidationMessages.AccountNotFound, request.AccountId));

        if (!account.IsActive)
            throw new BankingApplicationException(ValidationMessages.AccountNotActive);

        // Authorization: Users can only withdraw from their own accounts, staff can withdraw from any
        if (!_currentUserService.IsStaff && account.UserId != _currentUserService.UserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to withdraw from account {AccountId} owned by {OwnerId}",
                _currentUserService.UserId,
                account.Id,
                account.UserId);

            throw new ForbiddenException("You can only withdraw from your own accounts");
        }

        var withdrawAmount = new Money(request.Amount, "USD");

        // Check sufficient funds
        if (account.Balance.IsLessThan(withdrawAmount))
        {
            throw new InsufficientFundsException(
                string.Format(ValidationMessages.InsufficientFundsDetail,
                    account.Balance.Amount,
                    request.Amount));
        }

        var newBalance = account.Balance - withdrawAmount;
        account.Balance = newBalance;

        // Create transaction record
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId,
            TransactionType = TransactionType.Withdrawal,
            Amount = withdrawAmount,
            BalanceAfter = newBalance,
            Description = request.Description ?? "Cash withdrawal",
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ReferenceNumber = $"WTH{DateTime.UtcNow.Ticks}{Random.Shared.Next(1000, 9999)}"
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Withdrawal of {Amount} from account {AccountId}, new balance: {Balance}",
            request.Amount,
            request.AccountId,
            newBalance.Amount);

        return Unit.Value;
    }
}
