namespace BankingSystem.Application.DTOs.Accounts;

public class AccountDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsActive { get; set; }
    public string? IBAN { get; set; }
    public string? BIC { get; set; }
}
