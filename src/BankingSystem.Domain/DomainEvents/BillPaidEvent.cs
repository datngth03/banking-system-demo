using BankingSystem.Domain.Interfaces;
using MediatR;

namespace BankingSystem.Domain.DomainEvents;

public class BillPaidEvent : IDomainEvent, INotification
{
    public Guid AggregateId { get; }
    public DateTime OccurredOn { get; }
    public Guid BillId { get; }
    public Guid AccountId { get; }
    public decimal Amount { get; }
    public string Biller { get; }
    public string BillNumber { get; } = string.Empty;

    public BillPaidEvent(
        Guid billId,
        Guid accountId,
        decimal amount,
        string biller,
        string billNumber)
    {
        AggregateId = billId;
        OccurredOn = DateTime.UtcNow;
        BillId = billId;
        AccountId = accountId;
        Amount = amount;
        Biller = biller;
        BillNumber = billNumber;
    }
}
