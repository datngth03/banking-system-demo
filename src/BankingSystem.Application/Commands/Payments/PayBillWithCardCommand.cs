namespace BankingSystem.Application.Commands.Payments;

using BankingSystem.Application.DTOs.Payments;
using MediatR;

/// <summary>
/// Command to pay a bill using a card
/// </summary>
public class PayBillWithCardCommand : IRequest<PaymentResponse>
{
    /// <summary>
    /// Bill ID to pay
    /// </summary>
    public Guid BillId { get; set; }

    /// <summary>
    /// Stripe payment method ID
    /// </summary>
    public string PaymentMethodId { get; set; } = string.Empty;

    /// <summary>
    /// Optional metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Email for receipt
    /// </summary>
    public string? ReceiptEmail { get; set; }
}
