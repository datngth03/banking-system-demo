namespace BankingSystem.Application.DTOs.Cards;

public class CardDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public string CVV { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? BlockedAt { get; set; }
    public string? BlockedReason { get; set; }
}
