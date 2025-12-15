namespace BankingSystem.Application.Events;

using MediatR;

/// <summary>
/// Event published when an account is successfully created.
/// This event is used by the Outbox pattern to send notifications and emails.
/// </summary>
public class AccountCreatedEvent : INotification
{
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserFirstName { get; set; }
    public string? UserLastName { get; set; }
    
    // Account Information
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? IBAN { get; set; }
    public string? BIC { get; set; }
    public DateTime CreatedAt { get; set; }
}
