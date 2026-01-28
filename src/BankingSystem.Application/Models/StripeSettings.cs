namespace BankingSystem.Application.Models;

/// <summary>
/// Stripe payment gateway configuration settings
/// </summary>
public class StripeSettings
{
    /// <summary>
    /// Stripe Secret API Key (sk_test_* for test, sk_live_* for production)
    /// NEVER commit this to source code - use environment variables or Key Vault
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Stripe Publishable API Key (pk_test_* for test, pk_live_* for production)
    /// Safe to expose to frontend for payment processing
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// Webhook signing secret from Stripe dashboard (whsec_*)
    /// Used to verify webhook authenticity
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use Stripe Test Mode (recommended for development)
    /// In test mode, use test cards like 4242-4242-4242-4242
    /// </summary>
    public bool IsTestMode { get; set; } = true;

    /// <summary>
    /// Success URL for redirect after payment (for Hosted Checkout)
    /// Example: https://yourdomain.com/payment-success?session_id={CHECKOUT_SESSION_ID}
    /// </summary>
    public string SuccessUrl { get; set; } = string.Empty;

    /// <summary>
    /// Cancellation URL for redirect if payment is cancelled
    /// Example: https://yourdomain.com/payment-cancelled
    /// </summary>
    public string CancelUrl { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of retry attempts for failed API calls (default: 3)
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Request timeout in milliseconds (default: 30000ms = 30s)
    /// </summary>
    public int RequestTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Webhook signature validation timeout in minutes (default: 5)
    /// Prevents replay attacks by rejecting old webhook signatures
    /// </summary>
    public int WebhookSignatureValidityMinutes { get; set; } = 5;
}
