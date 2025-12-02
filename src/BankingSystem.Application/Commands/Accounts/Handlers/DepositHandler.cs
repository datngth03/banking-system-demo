using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Commands.Accounts;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Enums;
using BankingSystem.Domain.ValueObjects;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankingSystem.Application.Commands.Accounts.Handlers;

public class DepositHandler : IRequestHandler<DepositCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DepositHandler> _logger;

    public DepositHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<DepositHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Unit> Handle(DepositCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new ValidationFailureException(ValidationMessages.InvalidAmount);

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

        if (account == null)
            throw new NotFoundException(string.Format(ValidationMessages.AccountNotFound, request.AccountId));

        if (!account.IsActive)
            throw new BankingApplicationException(ValidationMessages.AccountNotActive);

        // Authorization: Users can only deposit to their own accounts, staff can deposit to any
        if (!_currentUserService.IsStaff && account.UserId != _currentUserService.UserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to deposit to account {AccountId} owned by {OwnerId}",
                _currentUserService.UserId,
                account.Id,
                account.UserId);

            throw new ForbiddenException("You can only deposit to your own accounts");
        }

        var depositAmount = new Money(request.Amount, "USD");
        var newBalance = account.Balance + depositAmount;
        account.Balance = newBalance;

        // Create transaction record
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId,
            TransactionType = TransactionType.Deposit,
            Amount = depositAmount,
            BalanceAfter = newBalance,
            Description = request.Description ?? "Cash deposit",
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ReferenceNumber = request.ReferenceNumber ?? $"DEP{DateTime.UtcNow.Ticks}{Random.Shared.Next(1000, 9999)}"
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deposit of {Amount} to account {AccountId}, new balance: {Balance}",
            request.Amount,
            request.AccountId,
            newBalance.Amount);

        return Unit.Value;
    }
}
