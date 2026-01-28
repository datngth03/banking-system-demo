namespace BankingSystem.Domain.Enums;

/// <summary>
/// Payment processing status
/// Tracks the lifecycle of a payment through Stripe integration
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment has been initiated but not yet processed by Stripe
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment is being processed by Stripe (waiting for webhook confirmation)
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Payment has been successfully completed
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// Payment failed (declined card, insufficient funds, etc.)
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Payment was cancelled by user before completion
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Payment has been refunded (full or partial)
    /// </summary>
    Refunded = 5,

    /// <summary>
    /// Payment is in dispute (chargeback initiated)
    /// </summary>
    Disputed = 6
}
