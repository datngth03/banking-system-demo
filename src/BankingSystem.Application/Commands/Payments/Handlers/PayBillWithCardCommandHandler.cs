namespace BankingSystem.Application.Commands.Payments.Handlers;

using BankingSystem.Application.DTOs.Payments;
using BankingSystem.Application.Events;
using BankingSystem.Application.Interfaces;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Enums;
using BankingSystem.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Handles PayBillWithCardCommand - pays a bill using Stripe payment
/// </summary>
public class PayBillWithCardCommandHandler : IRequestHandler<PayBillWithCardCommand, PaymentResponse>
{
    private readonly IPaymentService _paymentService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<PayBillWithCardCommandHandler> _logger;

    public PayBillWithCardCommandHandler(
        IPaymentService paymentService,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IEventPublisher eventPublisher,
        ILogger<PayBillWithCardCommandHandler> logger)
    {
        _paymentService = paymentService;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<PaymentResponse> Handle(PayBillWithCardCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing PayBillWithCardCommand: BillId={BillId}",
            request.BillId);

        try
        {
            var userId = _currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("PayBillWithCardCommand: User not authenticated");
                return new PaymentResponse
                {
                    Status = "failed",
                    ErrorMessage = "User authentication required",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            // Fetch the bill
            var bill = await _dbContext.Bills
                .FirstOrDefaultAsync(b => b.Id == request.BillId, cancellationToken);

            if (bill == null)
            {
                _logger.LogWarning("Bill not found: BillId={BillId}", request.BillId);
                return new PaymentResponse
                {
                    Status = "failed",
                    ErrorMessage = "Bill not found",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            // Verify user owns this bill's account
            var account = await _dbContext.Accounts
                .FirstOrDefaultAsync(a => a.Id == bill.AccountId, cancellationToken);

            if (account == null || account.UserId != userId)
            {
                _logger.LogWarning(
                    "Unauthorized access to bill: BillId={BillId}, UserId={UserId}",
                    request.BillId, userId);
                return new PaymentResponse
                {
                    Status = "failed",
                    ErrorMessage = "Unauthorized access to this bill",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            // Check if bill is already paid
            if (bill.IsPaid)
            {
                _logger.LogWarning("Bill already paid: BillId={BillId}", request.BillId);
                return new PaymentResponse
                {
                    Status = "failed",
                    ErrorMessage = "Bill is already paid",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            // Create Stripe PaymentIntent
            var amount = (long)(bill.Amount.Amount * 100);
            var idempotencyKey = GenerateIdempotencyKey(userId ?? Guid.Empty, bill.Id, bill.Amount.Amount);

            var paymentResponse = await _paymentService.CreatePaymentIntentAsync(
                amount,
                "USD", // Could make this dynamic
                request.PaymentMethodId,
                $"Bill payment: {bill.Description}",
                request.Metadata ?? new Dictionary<string, string> { { "bill_id", bill.Id.ToString() } },
                idempotencyKey,
                request.ReceiptEmail,
                true,
                cancellationToken);

            if (paymentResponse.Status == "failed")
            {
                _logger.LogError(
                    "Bill payment failed: BillId={BillId}, Error={Error}",
                    request.BillId, paymentResponse.ErrorMessage);

                // Publish payment failed event
                await _eventPublisher.PublishAsync(
                    new PaymentFailedEvent
                    {
                        TransactionId = Guid.NewGuid(),
                        UserId = userId ?? Guid.Empty,
                        Amount = amount,
                        Currency = "USD",
                        StripePaymentId = paymentResponse.StripePaymentId,
                        FailureReason = paymentResponse.ErrorMessage ?? "Unknown error",
                        ErrorCode = paymentResponse.ErrorCode,
                        PaymentMethod = "Card",
                        FailedAt = DateTime.UtcNow
                    },
                    cancellationToken);

                return paymentResponse;
            }

            // Create Transaction record
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountId = account.Id,
                Type = TransactionType.BillPayment,
                Amount = bill.Amount,
                Currency = "USD",
                Description = $"Bill payment: {bill.Description}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = "processing",
                Reference = $"BLP{DateTime.UtcNow.Ticks}",
                BalanceAfter = new Money(0, "USD"),
                TransactionDate = DateTime.UtcNow,
                TransactionType = TransactionType.BillPayment
                // Stripe-specific fields (will be added in migration)
                // StripePaymentId = paymentResponse.StripePaymentId,
                // PaymentStatus = PaymentStatus.Processing,
                // ExternalReferenceId = paymentResponse.StripePaymentId
            };

            _dbContext.Transactions.Add(transaction);

            // Update bill to pending payment
            bill.IsPaid = false; // Will be set to true when webhook confirms
            // bill.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            paymentResponse.TransactionId = transaction.Id;

            // Publish payment initiated event
            await _eventPublisher.PublishAsync(
                new PaymentInitiatedEvent
                {
                    TransactionId = transaction.Id,
                    UserId = userId ?? Guid.Empty,
                    Amount = amount,
                    Currency = "USD",
                    PaymentMethod = "Card",
                    Description = $"Bill payment: {bill.Description}",
                    InitiatedAt = DateTime.UtcNow
                },
                cancellationToken);

            _logger.LogInformation(
                "Bill payment initiated: BillId={BillId}, TransactionId={TransactionId}, PaymentId={PaymentId}",
                request.BillId, transaction.Id, paymentResponse.StripePaymentId);

            return paymentResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in PayBillWithCardCommandHandler: {Message}", ex.Message);
            return new PaymentResponse
            {
                Status = "failed",
                ErrorMessage = "An unexpected error occurred",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }

    private string GenerateIdempotencyKey(Guid userId, Guid billId, decimal amount)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return $"{userId:N}-{billId:N}-{(long)(amount * 100)}-{timestamp}";
    }
}
