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
/// Handles RefundPaymentCommand - processes payment refunds via Stripe
/// </summary>
public class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand, PaymentResponse>
{
    private readonly IPaymentService _paymentService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<RefundPaymentCommandHandler> _logger;

    public RefundPaymentCommandHandler(
        IPaymentService paymentService,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IEventPublisher eventPublisher,
        ILogger<RefundPaymentCommandHandler> logger)
    {
        _paymentService = paymentService;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<PaymentResponse> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing RefundPaymentCommand: TransactionId={TransactionId}, Amount={Amount}",
            request.TransactionId, request.Amount);

        try
        {
            var userId = _currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("RefundPaymentCommand: User not authenticated");
                return new PaymentResponse
                {
                    Status = "failed",
                    ErrorMessage = "User authentication required",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            // Fetch the transaction
            var transaction = await _dbContext.Transactions
                .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found: TransactionId={TransactionId}", request.TransactionId);
                return new PaymentResponse
                {
                    Status = "failed",
                    ErrorMessage = "Transaction not found",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            // Verify user owns this transaction
            if (transaction.UserId != userId)
            {
                _logger.LogWarning(
                    "Unauthorized access to transaction: TransactionId={TransactionId}, UserId={UserId}",
                    request.TransactionId, userId);
                return new PaymentResponse
                {
                    Status = "failed",
                    ErrorMessage = "Unauthorized access to this transaction",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            // Check if transaction is refundable
            if (string.IsNullOrEmpty(transaction.StripePaymentId))
            {
                _logger.LogWarning(
                    "Transaction not refundable (no Stripe ID): TransactionId={TransactionId}",
                    request.TransactionId);
                return new PaymentResponse
                {
                    Status = "failed",
                    ErrorMessage = "This transaction cannot be refunded",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            // Check transaction status
            if (transaction.Status == "failed" || transaction.Status == "refunded")
            {
                _logger.LogWarning(
                    "Transaction not refundable (status={Status}): TransactionId={TransactionId}",
                    transaction.Status, request.TransactionId);
                return new PaymentResponse
                {
                    Status = "failed",
                    ErrorMessage = $"Cannot refund transaction with status: {transaction.Status}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            // Process refund via Stripe
            var refundAmount = request.Amount ?? (long)(transaction.Amount.Amount * 100);
            var refundResponse = await _paymentService.RefundPaymentAsync(
                transaction.StripePaymentId,
                refundAmount,
                request.Reason,
                new Dictionary<string, string>
                {
                    { "transaction_id", transaction.Id.ToString() },
                    { "original_payment_id", transaction.StripePaymentId }
                },
                cancellationToken);

            if (refundResponse.Status == "failed")
            {
                _logger.LogError(
                    "Refund failed: TransactionId={TransactionId}, Error={Error}",
                    request.TransactionId, refundResponse.ErrorMessage);

                // Publish refund failure event (optional - not in the original design but useful)
                // await _eventPublisher.PublishAsync(
                //     new PaymentFailedEvent { ... },
                //     cancellationToken);

                return refundResponse;
            }

            // Update transaction status
            transaction.Status = "refunded";
            transaction.UpdatedAt = DateTime.UtcNow;
            // Note: Will update PaymentStatus field once migration is applied
            // transaction.PaymentStatus = PaymentStatus.Refunded;

            await _dbContext.SaveChangesAsync(cancellationToken);

            refundResponse.TransactionId = transaction.Id;

            // Publish refund event
            await _eventPublisher.PublishAsync(
                new PaymentRefundedEvent
                {
                    TransactionId = transaction.Id,
                    UserId = transaction.UserId ?? Guid.Empty,
                    Amount = (long)(transaction.Amount.Amount * 100),
                    RefundAmount = refundAmount,
                    Currency = transaction.Currency,
                    StripePaymentId = transaction.StripePaymentId ?? string.Empty,
                    RefundId = refundResponse.StripePaymentId ?? string.Empty,
                    RefundReason = request.Reason ?? "Customer requested refund",
                    RefundedAt = DateTime.UtcNow
                },
                cancellationToken);

            _logger.LogInformation(
                "Refund processed successfully: TransactionId={TransactionId}, RefundId={RefundId}",
                request.TransactionId, refundResponse.StripePaymentId);

            return refundResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in RefundPaymentCommandHandler: {Message}", ex.Message);
            return new PaymentResponse
            {
                Status = "failed",
                ErrorMessage = "An unexpected error occurred",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
