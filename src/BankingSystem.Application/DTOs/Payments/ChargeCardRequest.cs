namespace BankingSystem.Application.DTOs.Payments;

/// <summary>
/// Request DTO for charging a card directly
/// </summary>
public class ChargeCardRequest
{
    /// <summary>
    /// Amount to charge (in cents, e.g., 1000 = $10.00)
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Currency code (ISO 4217), e.g., "USD", "EUR"
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Stripe payment method token (created by Stripe.js on frontend)
    /// </summary>
    public string PaymentMethodId { get; set; } = string.Empty;

    /// <summary>
    /// Optional description for the charge
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional metadata to store with the charge
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Confirmation token for Strong Customer Authentication (3D Secure)
    /// </summary>
    public string? ConfirmationToken { get; set; }

    /// <summary>
    /// Whether to automatically confirm the payment (default: true)
    /// If false, client must confirm payment after receiving secret
    /// </summary>
    public bool AutoConfirm { get; set; } = true;

    /// <summary>
    /// Email address for payment receipt
    /// </summary>
    public string? ReceiptEmail { get; set; }
}
