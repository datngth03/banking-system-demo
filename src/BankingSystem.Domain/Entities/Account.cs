using BankingSystem.Domain.Enums;
using BankingSystem.Domain.Interfaces;
using BankingSystem.Domain.ValueObjects;

namespace BankingSystem.Domain.Entities;

public class Account : IAggregateRoot
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public Money Balance { get; set; } = new Money(0, "USD");
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? IBAN { get; set; }
    public string? BIC { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Card> Cards { get; set; } = new List<Card>();
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();

    // Domain events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
