namespace BankingSystem.Application.Commands.Payments.Handlers;

using BankingSystem.Application.DTOs.Payments;
using BankingSystem.Application.Events;
using BankingSystem.Application.Interfaces;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Enums;
using BankingSystem.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles ChargeCardCommand - processes direct card charges via Stripe
/// </summary>
public class ChargeCardCommandHandler : IRequestHandler<ChargeCardCommand, PaymentResponse>
{
    private readonly IPaymentService _paymentService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ChargeCardCommandHandler> _logger;

    public ChargeCardCommandHandler(
        IPaymentService paymentService,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IEventPublisher eventPublisher,
        ILogger<ChargeCardCommandHandler> logger)
    {
        _paymentService = paymentService;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<PaymentResponse> Handle(ChargeCardCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing ChargeCardCommand: Amount={Amount}, Currency={Currency}",
            request.Amount, request.Currency);

        try
        {
            var userId = _currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("ChargeCardCommand: User not authenticated");
                return new PaymentResponse
                {
                    Status = "failed",
                    ErrorMessage = "User authentication required",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            // Generate idempotency key to prevent duplicate charges
            var idempotencyKey = GenerateIdempotencyKey(userId ?? Guid.Empty, request.Amount, request.Currency);

            // Create Stripe PaymentIntent
            var paymentResponse = await _paymentService.CreatePaymentIntentAsync(
                request.Amount,
                request.Currency,
                request.PaymentMethodId,
                request.Description,
                request.Metadata,
                idempotencyKey,
                request.ReceiptEmail,
                request.AutoConfirm,
                cancellationToken);

            if (paymentResponse.Status == "failed")
            {
                _logger.LogError(
                    "Card charge failed: PaymentId={PaymentId}, Error={Error}",
                    paymentResponse.StripePaymentId, paymentResponse.ErrorMessage);

                // Publish payment failed event
                await _eventPublisher.PublishAsync(
                    new PaymentFailedEvent
                    {
                        TransactionId = Guid.NewGuid(),
                        UserId = userId ?? Guid.Empty,
                        Amount = request.Amount,
                        Currency = request.Currency,
                        StripePaymentId = paymentResponse.StripePaymentId,
                        FailureReason = paymentResponse.ErrorMessage ?? "Unknown error",
                        ErrorCode = paymentResponse.ErrorCode,
                        PaymentMethod = "Card",
                        FailedAt = DateTime.UtcNow
                    },
                    cancellationToken);

                return paymentResponse;
            }

            // Create Transaction record for audit trail
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = TransactionType.CardCharge,
                Amount = new Money((decimal)request.Amount / 100, request.Currency),
                Currency = request.Currency,
                Description = request.Description ?? "Card charge via Stripe",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = "pending",
                Reference = $"CHG{DateTime.UtcNow.Ticks}",
                AccountId = Guid.NewGuid(), // Generate placeholder GUID for card charges
                BalanceAfter = new Money(0, request.Currency),
                TransactionDate = DateTime.UtcNow,
                TransactionType = TransactionType.CardCharge
                // Stripe-specific fields (will be added in migration)
                // StripePaymentId = paymentResponse.StripePaymentId,
                // PaymentStatus = PaymentStatus.Pending,
                // ExternalReferenceId = paymentResponse.StripePaymentId
            };

            _dbContext.Transactions.Add(transaction);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Return response with transaction ID
            paymentResponse.TransactionId = transaction.Id;

            // Publish payment initiated event
            await _eventPublisher.PublishAsync(
                new PaymentInitiatedEvent
                {
                    TransactionId = transaction.Id,
                    UserId = userId ?? Guid.Empty,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    PaymentMethod = "Card",
                    Description = request.Description,
                    InitiatedAt = DateTime.UtcNow
                },
                cancellationToken);

            // For immediately confirmed payments, also publish processed event
            if (paymentResponse.Status == "succeeded")
            {
                await _eventPublisher.PublishAsync(
                    new PaymentProcessedEvent
                    {
                        TransactionId = transaction.Id,
                        UserId = userId ?? Guid.Empty,
                        Amount = request.Amount,
                        Currency = request.Currency,
                        StripePaymentId = paymentResponse.StripePaymentId ?? string.Empty,
                        PaymentStatus = "succeeded",
                        PaymentMethod = "Card",
                        ReceiptUrl = null,
                        ProcessedAt = DateTime.UtcNow
                    },
                    cancellationToken);
            }

            _logger.LogInformation(
                "Card charge initiated successfully: TransactionId={TransactionId}, PaymentId={PaymentId}",
                transaction.Id, paymentResponse.StripePaymentId);

            return paymentResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in ChargeCardCommandHandler: {Message}", ex.Message);
            return new PaymentResponse
            {
                Status = "failed",
                ErrorMessage = "An unexpected error occurred",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Generates idempotency key to prevent duplicate charges
    /// </summary>
    private string GenerateIdempotencyKey(Guid userId, long amount, string currency)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return $"{userId:N}-{amount}-{currency}-{timestamp}";
    }
}
