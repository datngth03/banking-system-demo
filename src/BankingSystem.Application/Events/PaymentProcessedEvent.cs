namespace BankingSystem.Application.Events;

using MediatR;

/// <summary>
/// Event published when a payment is successfully processed by Stripe
/// </summary>
public class PaymentProcessedEvent : INotification
{
    public Guid TransactionId { get; set; }
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserFirstName { get; set; }
    public string? UserLastName { get; set; }
    
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string StripePaymentId { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = "succeeded";
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReceiptUrl { get; set; }
    public DateTime ProcessedAt { get; set; }
}
