namespace BankingSystem.Application.Interfaces;

using BankingSystem.Application.DTOs.Payments;

/// <summary>
/// Interface for payment gateway operations (Stripe integration)
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Creates a Stripe PaymentIntent for a charge
    /// </summary>
    /// <param name="amount">Amount in cents (e.g., 1000 = $10.00)</param>
    /// <param name="currency">Currency code (e.g., "USD")</param>
    /// <param name="paymentMethodId">Stripe payment method ID</param>
    /// <param name="description">Payment description</param>
    /// <param name="metadata">Optional metadata to attach</param>
    /// <param name="idempotencyKey">Idempotency key to prevent duplicate charges</param>
    /// <param name="receiptEmail">Email for payment receipt</param>
    /// <param name="autoConfirm">Auto-confirm payment or wait for webhook</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PaymentResponse with status and client secret if needed</returns>
    Task<PaymentResponse> CreatePaymentIntentAsync(
        long amount,
        string currency,
        string paymentMethodId,
        string? description = null,
        Dictionary<string, string>? metadata = null,
        string? idempotencyKey = null,
        string? receiptEmail = null,
        bool autoConfirm = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a payment after customer completes 3D Secure challenge
    /// </summary>
    /// <param name="paymentIntentId">Stripe PaymentIntent ID</param>
    /// <param name="paymentMethodId">Stripe payment method ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated PaymentResponse</returns>
    Task<PaymentResponse> ConfirmPaymentAsync(
        string paymentIntentId,
        string paymentMethodId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refunds a payment (full or partial)
    /// </summary>
    /// <param name="paymentId">Stripe charge/payment ID to refund</param>
    /// <param name="amount">Optional amount for partial refund (in cents)</param>
    /// <param name="reason">Refund reason (e.g., "Customer requested")</param>
    /// <param name="metadata">Optional metadata for the refund</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PaymentResponse with refund status</returns>
    Task<PaymentResponse> RefundPaymentAsync(
        string paymentId,
        long? amount = null,
        string? reason = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves payment status from Stripe
    /// </summary>
    /// <param name="paymentId">Stripe payment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current PaymentResponse</returns>
    Task<PaymentResponse> GetPaymentStatusAsync(
        string paymentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates webhook signature from Stripe
    /// </summary>
    /// <param name="signatureHeader">Stripe-Signature header value</param>
    /// <param name="payload">Raw webhook payload</param>
    /// <returns>True if signature is valid</returns>
    bool ValidateWebhookSignature(string signatureHeader, string payload);
}
