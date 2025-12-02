namespace BankingSystem.Application.DTOs.Transactions;

/// <summary>
/// Transaction receipt details
/// </summary>
public class TransactionReceiptDto
{
    public Guid TransactionId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty;

    // Account Information
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;

    // Transaction Details
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;

    // Related Account (for transfers)
    public string? RelatedAccountNumber { get; set; }

    // Receipt Metadata
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string ReceiptNumber { get; set; } = string.Empty;

    // Status
    public string Status { get; set; } = "Completed";
}
