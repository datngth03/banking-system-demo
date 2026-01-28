namespace BankingSystem.Application.DTOs.Payments;

/// <summary>
/// Request DTO for paying a bill with a card
/// </summary>
public class PayBillWithCardRequest
{
    /// <summary>
    /// Bill ID to pay
    /// </summary>
    public Guid BillId { get; set; }

    /// <summary>
    /// Stripe payment method token (created by Stripe.js on frontend)
    /// </summary>
    public string PaymentMethodId { get; set; } = string.Empty;

    /// <summary>
    /// Optional payment metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Email address for payment receipt
    /// </summary>
    public string? ReceiptEmail { get; set; }
}
