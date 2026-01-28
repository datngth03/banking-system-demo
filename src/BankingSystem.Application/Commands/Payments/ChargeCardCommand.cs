namespace BankingSystem.Application.Commands.Payments;

using BankingSystem.Application.DTOs.Payments;
using MediatR;

/// <summary>
/// Command to charge a card directly
/// </summary>
public class ChargeCardCommand : IRequest<PaymentResponse>
{
    /// <summary>
    /// Amount to charge in cents
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Currency code (e.g., "USD")
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Stripe payment method ID
    /// </summary>
    public string PaymentMethodId { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Email for receipt
    /// </summary>
    public string? ReceiptEmail { get; set; }

    /// <summary>
    /// Confirmation token for 3D Secure
    /// </summary>
    public string? ConfirmationToken { get; set; }

    /// <summary>
    /// Auto-confirm payment
    /// </summary>
    public bool AutoConfirm { get; set; } = true;
}
