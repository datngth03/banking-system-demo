namespace BankingSystem.Application.Commands.Payments;

using BankingSystem.Application.DTOs.Payments;
using MediatR;

/// <summary>
/// Command to refund a payment
/// </summary>
public class RefundPaymentCommand : IRequest<PaymentResponse>
{
    /// <summary>
    /// Transaction ID to refund
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Optional refund reason
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Optional amount for partial refund (in cents)
    /// </summary>
    public long? Amount { get; set; }
}
