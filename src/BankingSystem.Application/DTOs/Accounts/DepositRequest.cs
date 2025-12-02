namespace BankingSystem.Application.DTOs.Accounts;

public class DepositRequest
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? ReferenceNumber { get; set; }
}
