namespace BankingSystem.Application.DTOs.Accounts;

public class WithdrawRequest
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}
