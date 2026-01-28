namespace BankingSystem.Application.Events;

using MediatR;

/// <summary>
/// Event published when a payment is refunded
/// </summary>
public class PaymentRefundedEvent : INotification
{
    public Guid TransactionId { get; set; }
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserFirstName { get; set; }
    public string? UserLastName { get; set; }
    
    public long Amount { get; set; }
    public long RefundAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string StripePaymentId { get; set; } = string.Empty;
    public string RefundId { get; set; } = string.Empty;
    public string RefundReason { get; set; } = string.Empty;
    public DateTime RefundedAt { get; set; }
}
