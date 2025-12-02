namespace BankingSystem.Domain.Interfaces;

public interface IDomainEvent
{
    Guid AggregateId { get; }
    DateTime OccurredOn { get; }
}
