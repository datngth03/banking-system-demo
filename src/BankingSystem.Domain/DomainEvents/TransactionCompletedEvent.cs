using BankingSystem.Domain.Interfaces;
using MediatR;

namespace BankingSystem.Domain.DomainEvents;

public class TransactionCompletedEvent : IDomainEvent, INotification
{
    public Guid AggregateId { get; }
    public DateTime OccurredOn { get; }
    public Guid AccountId { get; }
    public decimal Amount { get; }
    public string TransactionType { get; }
    public string Description { get; }

    public TransactionCompletedEvent(Guid transactionId, Guid accountId, decimal amount, string transactionType, string description)
    {
        AggregateId = transactionId;
        OccurredOn = DateTime.UtcNow;
        AccountId = accountId;
        Amount = amount;
        TransactionType = transactionType;
        Description = description;
    }
}
