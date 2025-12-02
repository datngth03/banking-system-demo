using BankingSystem.Domain.Interfaces;
using BankingSystem.Domain.ValueObjects;

namespace BankingSystem.Domain.Entities;

public class Bill : IEntity
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public string Biller { get; set; } = string.Empty;
    public Money Amount { get; set; } = new Money(0, "USD");
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public bool IsPaid { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Account? Account { get; set; }
}
