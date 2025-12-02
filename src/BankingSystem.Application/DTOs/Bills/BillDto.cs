namespace BankingSystem.Application.DTOs.Bills;

public class BillDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public string Biller { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public bool IsPaid { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
