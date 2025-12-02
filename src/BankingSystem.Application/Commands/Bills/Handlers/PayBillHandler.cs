using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Commands.Bills;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using BankingSystem.Application.Exceptions;
using BankingSystem.Domain.Exceptions;
using BankingSystem.Application.Constants;
using Microsoft.Extensions.Logging;

namespace BankingSystem.Application.Commands.Bills.Handlers;

public class PayBillHandler : IRequestHandler<PayBillCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PayBillHandler> _logger;

    public PayBillHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<PayBillHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Unit> Handle(PayBillCommand request, CancellationToken cancellationToken)
    {
        var bill = await _context.Bills
            .Include(b => b.Account)
            .FirstOrDefaultAsync(b => b.Id == request.BillId, cancellationToken);

        if (bill == null)
            throw new NotFoundException(string.Format(ValidationMessages.BillNotFound, request.BillId));

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

        if (account == null)
            throw new NotFoundException(string.Format(ValidationMessages.AccountNotFound, request.AccountId));

        // Authorization: Users can only pay bills from their own accounts, staff can pay from any
        if (!_currentUserService.IsStaff && account.UserId != _currentUserService.UserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to pay bill from account {AccountId} owned by {OwnerId}",
                _currentUserService.UserId,
                account.Id,
                account.UserId);

            throw new ForbiddenException("You can only pay bills from your own accounts");
        }

        // Also verify that the bill belongs to the account being charged
        if (bill.AccountId != request.AccountId)
        {
            _logger.LogWarning(
                "Attempt to pay bill {BillId} (belongs to account {BillAccountId}) from different account {RequestAccountId}",
                bill.Id,
                bill.AccountId,
                request.AccountId);

            throw new BankingApplicationException("Cannot pay a bill that doesn't belong to this account");
        }

        if (bill.IsPaid)
            throw new BankingApplicationException(ValidationMessages.BillAlreadyPaid);

        if (account.Balance.IsLessThan(bill.Amount))
            throw new InsufficientFundsException(
                string.Format(ValidationMessages.InsufficientFundsDetail, account.Balance.Amount, bill.Amount.Amount));

        // Update bill
        bill.IsPaid = true;
        bill.PaidDate = DateTime.UtcNow;

        // Update account balance
        account.Balance = account.Balance - bill.Amount;

        // Create transaction
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId,
            TransactionType = TransactionType.BillPayment,
            Amount = bill.Amount,
            BalanceAfter = account.Balance,
            Description = $"Bill payment to {bill.Biller} (Bill #{bill.BillNumber})",
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ReferenceNumber = GenerateReferenceNumber()
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Bill {BillId} paid: {Amount} from account {AccountId} by user {UserId}",
            bill.Id,
            bill.Amount.Amount,
            account.Id,
            _currentUserService.UserId);

        return Unit.Value;
    }

    private static string GenerateReferenceNumber()
    {
        return $"BILL{DateTime.UtcNow.Ticks}{Random.Shared.Next(1000, 9999)}";
    }
}
