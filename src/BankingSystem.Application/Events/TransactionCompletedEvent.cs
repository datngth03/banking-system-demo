namespace BankingSystem.Application.Events;

using MediatR;

/// <summary>
/// Event published when a transaction is successfully completed.
/// This event is used by the Outbox pattern to send notifications and emails.
/// </summary>
public class TransactionCompletedEvent : INotification
{
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserFirstName { get; set; }
    public string? UserLastName { get; set; }
    
    // Transaction Information
    public Guid TransactionId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public string TransactionReference { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
}
