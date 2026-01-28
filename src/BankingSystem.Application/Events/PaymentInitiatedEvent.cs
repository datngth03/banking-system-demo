namespace BankingSystem.Application.Events;

using MediatR;

/// <summary>
/// Event published when a payment transaction is initiated (before Stripe processing)
/// </summary>
public class PaymentInitiatedEvent : INotification
{
    public Guid TransactionId { get; set; }
    public Guid UserId { get; set; }
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime InitiatedAt { get; set; }
}
