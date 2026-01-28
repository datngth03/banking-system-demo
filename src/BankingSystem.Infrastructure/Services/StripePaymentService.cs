namespace BankingSystem.Infrastructure.Services;

using BankingSystem.Application.DTOs.Payments;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System;

/// <summary>
/// Stripe payment gateway implementation
/// Handles all payment operations through Stripe API in test mode
/// </summary>
public class StripePaymentService : IPaymentService
{
    private readonly StripeSettings _stripeSettings;
    private readonly ILogger<StripePaymentService> _logger;
    private readonly StripeClient _stripeClient;

    public StripePaymentService(
        IOptions<StripeSettings> stripeOptions,
        ILogger<StripePaymentService> logger)
    {
        _stripeSettings = stripeOptions.Value;
        _logger = logger;

        // Initialize Stripe client with API key
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
        _stripeClient = new StripeClient(_stripeSettings.SecretKey);
    }

    /// <summary>
    /// Creates a Stripe PaymentIntent for charging a card
    /// </summary>
    public async Task<PaymentResponse> CreatePaymentIntentAsync(
        long amount,
        string currency,
        string paymentMethodId,
        string? description = null,
        Dictionary<string, string>? metadata = null,
        string? idempotencyKey = null,
        string? receiptEmail = null,
        bool autoConfirm = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating Stripe PaymentIntent: Amount={Amount}, Currency={Currency}, IdempotencyKey={IdempotencyKey}",
                amount, currency, idempotencyKey);

            var options = new PaymentIntentCreateOptions
            {
                Amount = amount,
                Currency = currency.ToLower(),
                PaymentMethod = paymentMethodId,
                Confirm = autoConfirm,
                OffSession = true, // Stored payment method
                ReturnUrl = _stripeSettings.SuccessUrl,
                ReceiptEmail = receiptEmail,
                Description = description,
                Metadata = metadata ?? new Dictionary<string, string>()
            };

            // Set idempotency key for duplicate prevention
            var requestOptions = new RequestOptions
            {
                IdempotencyKey = idempotencyKey ?? GenerateIdempotencyKey()
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options, requestOptions, cancellationToken);

            _logger.LogInformation(
                "PaymentIntent created successfully: Id={PaymentIntentId}, Status={Status}",
                paymentIntent.Id, paymentIntent.Status);

            return new PaymentResponse
            {
                TransactionId = Guid.NewGuid(), // Will be overridden by caller
                StripePaymentId = paymentIntent.Id,
                Status = paymentIntent.Status,
                Amount = amount,
                Currency = currency,
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentStatus = paymentIntent.Status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(
                ex,
                "Stripe API error creating PaymentIntent: Code={Code}, Message={Message}",
                ex.StripeError?.Code, ex.StripeError?.Message);

            return new PaymentResponse
            {
                TransactionId = Guid.NewGuid(),
                Status = "failed",
                Amount = amount,
                Currency = currency,
                ErrorCode = ex.StripeError?.Code,
                ErrorMessage = ex.StripeError?.Message ?? ex.Message,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating PaymentIntent: {Message}", ex.Message);
            return new PaymentResponse
            {
                TransactionId = Guid.NewGuid(),
                Status = "failed",
                Amount = amount,
                Currency = currency,
                ErrorMessage = "An unexpected error occurred",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Confirms a payment after customer completes 3D Secure
    /// </summary>
    public async Task<PaymentResponse> ConfirmPaymentAsync(
        string paymentIntentId,
        string paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Confirming PaymentIntent: Id={PaymentIntentId}",
                paymentIntentId);

            var options = new PaymentIntentConfirmOptions
            {
                PaymentMethod = paymentMethodId
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.ConfirmAsync(paymentIntentId, options, null, cancellationToken);

            _logger.LogInformation(
                "PaymentIntent confirmed: Id={PaymentIntentId}, Status={Status}",
                paymentIntent.Id, paymentIntent.Status);

            return MapPaymentIntentToResponse(paymentIntent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(
                ex,
                "Stripe API error confirming PaymentIntent: Code={Code}, Message={Message}",
                ex.StripeError?.Code, ex.StripeError?.Message);

            return new PaymentResponse
            {
                StripePaymentId = paymentIntentId,
                Status = "failed",
                ErrorCode = ex.StripeError?.Code,
                ErrorMessage = ex.StripeError?.Message ?? ex.Message,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Refunds a payment (full or partial)
    /// </summary>
    public async Task<PaymentResponse> RefundPaymentAsync(
        string paymentId,
        long? amount = null,
        string? reason = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Refunding payment: PaymentId={PaymentId}, Amount={Amount}, Reason={Reason}",
                paymentId, amount, reason);

            var options = new RefundCreateOptions
            {
                Charge = paymentId,
                Amount = amount,
                Reason = reason != null ? MapRefundReason(reason) : "requested_by_customer",
                Metadata = metadata ?? new Dictionary<string, string>()
            };

            var service = new RefundService();
            var refund = await service.CreateAsync(options, null, cancellationToken);

            _logger.LogInformation(
                "Refund created successfully: RefundId={RefundId}, Status={Status}",
                refund.Id, refund.Status);

            return new PaymentResponse
            {
                TransactionId = Guid.NewGuid(),
                StripePaymentId = refund.Id,
                Status = refund.Status == "succeeded" ? "refunded" : refund.Status,
                Amount = refund.Amount,
                Currency = refund.Currency,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(
                ex,
                "Stripe API error refunding payment: Code={Code}, Message={Message}",
                ex.StripeError?.Code, ex.StripeError?.Message);

            return new PaymentResponse
            {
                StripePaymentId = paymentId,
                Status = "failed",
                ErrorCode = ex.StripeError?.Code,
                ErrorMessage = ex.StripeError?.Message ?? ex.Message,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Retrieves payment status from Stripe
    /// </summary>
    public async Task<PaymentResponse> GetPaymentStatusAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentId, null, null, cancellationToken);

            return MapPaymentIntentToResponse(paymentIntent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(
                ex,
                "Stripe API error retrieving payment status: Code={Code}",
                ex.StripeError?.Code);

            return new PaymentResponse
            {
                StripePaymentId = paymentId,
                Status = "unknown",
                ErrorCode = ex.StripeError?.Code,
                ErrorMessage = ex.StripeError?.Message ?? ex.Message,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Validates webhook signature from Stripe
    /// </summary>
    public bool ValidateWebhookSignature(string signatureHeader, string payload)
    {
        try
        {
            EventUtility.ValidateSignature(
                payload,
                signatureHeader,
                _stripeSettings.WebhookSecret);

            _logger.LogInformation("Webhook signature validated successfully");
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogWarning("Webhook signature validation failed: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Helper method to map Stripe PaymentIntent to PaymentResponse
    /// </summary>
    private PaymentResponse MapPaymentIntentToResponse(PaymentIntent paymentIntent)
    {
        return new PaymentResponse
        {
            TransactionId = Guid.NewGuid(),
            StripePaymentId = paymentIntent.Id,
            Status = MapPaymentIntentStatus(paymentIntent.Status),
            Amount = paymentIntent.Amount,
            Currency = paymentIntent.Currency,
            ClientSecret = paymentIntent.ClientSecret,
            PaymentIntentStatus = paymentIntent.Status,
            ErrorCode = paymentIntent.LastPaymentError?.Code,
            ErrorMessage = paymentIntent.LastPaymentError?.Message,
            CreatedAt = paymentIntent.Created,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Maps Stripe payment intent status to internal status
    /// </summary>
    private string MapPaymentIntentStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "requires_payment_method" => "pending",
            "requires_action" => "processing",
            "processing" => "processing",
            "requires_capture" => "processing",
            "succeeded" => "succeeded",
            "canceled" => "cancelled",
            _ => "failed"
        };
    }

    /// <summary>
    /// Maps refund reason to Stripe refund reason
    /// </summary>
    private string MapRefundReason(string reason)
    {
        return reason.ToLower() switch
        {
            "duplicate" => "duplicate",
            "fraudulent" => "fraudulent",
            "requested_by_customer" => "requested_by_customer",
            _ => "requested_by_customer"
        };
    }

    /// <summary>
    /// Generates an idempotency key to prevent duplicate charges
    /// </summary>
    private string GenerateIdempotencyKey()
    {
        return $"{DateTime.UtcNow.Ticks}-{Guid.NewGuid():N}";
    }
}
