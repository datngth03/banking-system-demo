namespace BankingSystem.Application.Events;

using MediatR;

/// <summary>
/// Event published when a bill payment is completed.
/// This event is used by the Outbox pattern to send notifications and emails.
/// </summary>
public class BillPaymentCompletedEvent : INotification
{
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserFirstName { get; set; }
    public string? UserLastName { get; set; }
    
    // Bill Information
    public Guid BillId { get; set; }
    public string Biller { get; set; } = string.Empty;
    public string BillNumber { get; set; } = string.Empty;
    
    // Transaction Information
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal NewBalance { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public DateTime? PaidDate { get; set; }
}
