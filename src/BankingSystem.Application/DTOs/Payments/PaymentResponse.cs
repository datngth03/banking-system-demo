namespace BankingSystem.Application.DTOs.Payments;

/// <summary>
/// Response DTO for payment operations
/// </summary>
public class PaymentResponse
{
    /// <summary>
    /// Unique transaction ID in the Banking System
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Stripe payment ID (charge ID, payment intent ID, etc.)
    /// </summary>
    public string? StripePaymentId { get; set; }

    /// <summary>
    /// Payment status (Succeeded, Failed, Processing, Pending, Refunded)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Amount in cents
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Currency code (e.g., "USD")
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Client secret for frontend payment confirmation (if 3D Secure required)
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Payment intent status (requires_action, succeeded, etc.)
    /// </summary>
    public string? PaymentIntentStatus { get; set; }

    /// <summary>
    /// Error message if payment failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code from Stripe if payment failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Payment timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
