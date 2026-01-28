namespace BankingSystem.Application.Events;

using MediatR;

/// <summary>
/// Event published when a payment fails (declined card, insufficient funds, etc.)
/// </summary>
public class PaymentFailedEvent : INotification
{
    public Guid TransactionId { get; set; }
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? StripePaymentId { get; set; }
    public string FailureReason { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
}
