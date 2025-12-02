using BankingSystem.Domain.Interfaces;
using MediatR;

namespace BankingSystem.Domain.DomainEvents;

public class AccountCreatedEvent : IDomainEvent, INotification
{
    public Guid AggregateId { get; }
    public DateTime OccurredOn { get; }
    public Guid UserId { get; }
    public string AccountNumber { get; }
    public string AccountType { get; }

    public AccountCreatedEvent(Guid accountId, Guid userId, string accountNumber, string accountType)
    {
        AggregateId = accountId;
        OccurredOn = DateTime.UtcNow;
        UserId = userId;
        AccountNumber = accountNumber;
        AccountType = accountType;
    }
}
