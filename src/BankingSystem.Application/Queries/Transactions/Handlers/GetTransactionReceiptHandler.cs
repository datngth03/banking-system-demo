using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.DTOs.Transactions;
using BankingSystem.Application.Queries.Transactions;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankingSystem.Application.Queries.Transactions.Handlers;

public class GetTransactionReceiptHandler : IRequestHandler<GetTransactionReceiptQuery, TransactionReceiptDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetTransactionReceiptHandler> _logger;

    public GetTransactionReceiptHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetTransactionReceiptHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<TransactionReceiptDto> Handle(GetTransactionReceiptQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Account)
                .ThenInclude(a => a!.User)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

        if (transaction == null)
        {
            throw new NotFoundException(
                string.Format(ValidationMessages.TransactionNotFound, request.TransactionId));
        }

        // Authorization: Users can only get receipts for their own transactions
        if (!_currentUserService.IsStaff &&
            transaction.Account!.UserId != _currentUserService.UserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to get receipt for transaction {TransactionId} owned by {OwnerId}",
                _currentUserService.UserId,
                transaction.Id,
                transaction.Account.UserId);

            throw new ForbiddenException("You can only view receipts for your own transactions");
        }

        // Get related account info if exists
        string? relatedAccountNumber = null;
        if (transaction.RelatedAccountId.HasValue)
        {
            relatedAccountNumber = await _context.Accounts
                .Where(a => a.Id == transaction.RelatedAccountId.Value)
                .Select(a => a.AccountNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var receipt = new TransactionReceiptDto
        {
            TransactionId = transaction.Id,
            ReferenceNumber = transaction.ReferenceNumber,
            TransactionDate = transaction.TransactionDate,
            TransactionType = transaction.TransactionType.ToString(),

            AccountNumber = transaction.Account!.AccountNumber,
            AccountHolderName = $"{transaction.Account.User!.FirstName} {transaction.Account.User.LastName}",

            Amount = transaction.Amount.Amount,
            Currency = transaction.Amount.Currency,
            BalanceAfter = transaction.BalanceAfter.Amount,
            Description = transaction.Description ?? string.Empty,

            RelatedAccountNumber = relatedAccountNumber,

            GeneratedAt = DateTime.UtcNow,
            ReceiptNumber = $"RCP-{transaction.ReferenceNumber}",
            Status = "Completed"
        };

        _logger.LogInformation(
            "Generated receipt for transaction {TransactionId} for user {UserId}",
            transaction.Id,
            _currentUserService.UserId);

        return receipt;
    }
}
