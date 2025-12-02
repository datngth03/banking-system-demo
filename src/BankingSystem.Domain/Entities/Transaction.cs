using BankingSystem.Domain.Enums;
using BankingSystem.Domain.Interfaces;
using BankingSystem.Domain.ValueObjects;

namespace BankingSystem.Domain.Entities;

public class Transaction : IEntity
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid? RelatedAccountId { get; set; }
    public TransactionType TransactionType { get; set; }
    public Money Amount { get; set; } = new Money(0, "USD");
    public Money BalanceAfter { get; set; } = new Money(0, "USD");
    public string Description { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Account? Account { get; set; }
    public Account? RelatedAccount { get; set; }
}
