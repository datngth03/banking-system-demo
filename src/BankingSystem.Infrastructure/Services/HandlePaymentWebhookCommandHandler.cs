namespace BankingSystem.Infrastructure.Services;

using BankingSystem.Application.Commands.Payments;
using BankingSystem.Application.Interfaces;
using BankingSystem.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Text.Json;

/// <summary>
/// Handles HandlePaymentWebhookCommand - processes Stripe webhook events
/// </summary>
public class HandlePaymentWebhookCommandHandler : IRequestHandler<HandlePaymentWebhookCommand, bool>
{
    private readonly IPaymentService _paymentService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<HandlePaymentWebhookCommandHandler> _logger;

    public HandlePaymentWebhookCommandHandler(
        IPaymentService paymentService,
        IApplicationDbContext dbContext,
        ILogger<HandlePaymentWebhookCommandHandler> logger)
    {
        _paymentService = paymentService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> Handle(HandlePaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing Stripe webhook");

        try
        {
            // Validate webhook signature
            if (!_paymentService.ValidateWebhookSignature(request.SignatureHeader, request.Payload))
            {
                _logger.LogWarning("Invalid webhook signature");
                return false;
            }

            // Parse webhook event
            var stripeEvent = EventUtility.ParseEvent(request.Payload);

            _logger.LogInformation("Webhook event received: Type={EventType}, Id={EventId}", stripeEvent.Type, stripeEvent.Id);

            // Route to appropriate handler
            var handled = stripeEvent.Type switch
            {
                "charge.succeeded" => await HandleChargeSucceeded(stripeEvent, cancellationToken),
                "charge.failed" => await HandleChargeFailed(stripeEvent, cancellationToken),
                "charge.refunded" => await HandleChargeRefunded(stripeEvent, cancellationToken),
                "charge.dispute.created" => await HandleDisputeCreated(stripeEvent, cancellationToken),
                "payment_intent.succeeded" => await HandlePaymentIntentSucceeded(stripeEvent, cancellationToken),
                "payment_intent.payment_failed" => await HandlePaymentIntentFailed(stripeEvent, cancellationToken),
                _ => HandleUnknownEvent(stripeEvent)
            };

            if (handled)
            {
                _logger.LogInformation("Webhook processed successfully: Type={EventType}", stripeEvent.Type);
            }
            else
            {
                _logger.LogWarning("Webhook event type not handled: Type={EventType}", stripeEvent.Type);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook: {Message}", ex.Message);
            return false;
        }
    }

    private async Task<bool> HandleChargeSucceeded(Event stripeEvent, CancellationToken cancellationToken)
    {
        var charge = stripeEvent.Data.Object as Charge;
        if (charge == null) return false;

        _logger.LogInformation("Charge succeeded: ChargeId={ChargeId}", charge.Id);

        // Find transaction by Stripe payment ID
        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.StripePaymentId == charge.Id, cancellationToken);

        if (transaction == null)
        {
            _logger.LogWarning("Transaction not found for charge: ChargeId={ChargeId}", charge.Id);
            return false;
        }

        // Update transaction status
        transaction.Status = "succeeded";
        transaction.UpdatedAt = DateTime.UtcNow;
        // transaction.PaymentStatus = PaymentStatus.Succeeded; // After migration

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<bool> HandleChargeFailed(Event stripeEvent, CancellationToken cancellationToken)
    {
        var charge = stripeEvent.Data.Object as Charge;
        if (charge == null) return false;

        _logger.LogWarning("Charge failed: ChargeId={ChargeId}, Reason={Reason}", charge.Id, charge.FailureCode);

        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.StripePaymentId == charge.Id, cancellationToken);

        if (transaction == null)
        {
            _logger.LogWarning("Transaction not found for failed charge: ChargeId={ChargeId}", charge.Id);
            return false;
        }

        transaction.Status = "failed";
        transaction.UpdatedAt = DateTime.UtcNow;
        // transaction.PaymentStatus = PaymentStatus.Failed; // After migration

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<bool> HandleChargeRefunded(Event stripeEvent, CancellationToken cancellationToken)
    {
        var charge = stripeEvent.Data.Object as Charge;
        if (charge == null) return false;

        _logger.LogInformation("Charge refunded: ChargeId={ChargeId}, RefundedAmount={Amount}", charge.Id, charge.AmountRefunded);

        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.StripePaymentId == charge.Id, cancellationToken);

        if (transaction == null)
        {
            _logger.LogWarning("Transaction not found for refunded charge: ChargeId={ChargeId}", charge.Id);
            return false;
        }

        transaction.Status = "refunded";
        transaction.UpdatedAt = DateTime.UtcNow;
        // transaction.PaymentStatus = PaymentStatus.Refunded; // After migration

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<bool> HandleDisputeCreated(Event stripeEvent, CancellationToken cancellationToken)
    {
        var dispute = stripeEvent.Data.Object as Dispute;
        if (dispute == null) return false;

        _logger.LogWarning("Chargeback dispute created: DisputeId={DisputeId}, ChargeId={ChargeId}", dispute.Id, dispute.ChargeId);

        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.StripePaymentId == dispute.ChargeId, cancellationToken);

        if (transaction == null)
        {
            _logger.LogWarning("Transaction not found for dispute: DisputeId={DisputeId}", dispute.Id);
            return false;
        }

        transaction.Status = "disputed";
        transaction.UpdatedAt = DateTime.UtcNow;
        // transaction.PaymentStatus = PaymentStatus.Disputed; // After migration

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<bool> HandlePaymentIntentSucceeded(Event stripeEvent, CancellationToken cancellationToken)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return false;

        _logger.LogInformation("PaymentIntent succeeded: Id={Id}", paymentIntent.Id);

        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.StripePaymentId == paymentIntent.Id, cancellationToken);

        if (transaction == null)
        {
            _logger.LogWarning("Transaction not found for PaymentIntent: Id={Id}", paymentIntent.Id);
            return false;
        }

        transaction.Status = "succeeded";
        transaction.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<bool> HandlePaymentIntentFailed(Event stripeEvent, CancellationToken cancellationToken)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return false;

        _logger.LogWarning("PaymentIntent failed: Id={Id}, Reason={Reason}", paymentIntent.Id, paymentIntent.LastPaymentError?.Message);

        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.StripePaymentId == paymentIntent.Id, cancellationToken);

        if (transaction == null)
        {
            _logger.LogWarning("Transaction not found for failed PaymentIntent: Id={Id}", paymentIntent.Id);
            return false;
        }

        transaction.Status = "failed";
        transaction.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private bool HandleUnknownEvent(Event stripeEvent)
    {
        _logger.LogInformation("Ignoring webhook event type: {EventType}", stripeEvent.Type);
        return true;
    }
}
