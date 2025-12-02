using BankingSystem.Domain.Enums;
using BankingSystem.Domain.Interfaces;

namespace BankingSystem.Domain.Entities;

public class Card : IEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    
    // Encrypted fields (stored in database)
    public string EncryptedCardNumber { get; set; } = string.Empty;
    public string EncryptedCVV { get; set; } = string.Empty;
    
    // Plain text fields (not stored in database)
    public string CardNumber { get; set; } = string.Empty;
    public string CVV { get; set; } = string.Empty;
    
    public string CardHolderName { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public CardStatus Status { get; set; } = CardStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime? BlockedAt { get; set; }
    public string? BlockedReason { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Account? Account { get; set; }

    // Computed properties for masked display
    public string MaskedCardNumber => MaskCardNumber(CardNumber);
    public string MaskedCVV => "***";

    private static string MaskCardNumber(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
            return cardNumber;

        var lastFour = cardNumber.Substring(cardNumber.Length - 4);
        var masked = new string('*', cardNumber.Length - 4);
        return masked + lastFour;
    }

    // Methods for encryption/decryption
    public void EncryptSensitiveData(Func<string, string> encryptFunc)
    {
        if (!string.IsNullOrEmpty(CardNumber))
            EncryptedCardNumber = encryptFunc(CardNumber);
        
        if (!string.IsNullOrEmpty(CVV))
            EncryptedCVV = encryptFunc(CVV);
    }

    public void DecryptSensitiveData(Func<string, string> decryptFunc)
    {
        if (!string.IsNullOrEmpty(EncryptedCardNumber))
            CardNumber = decryptFunc(EncryptedCardNumber);
        
        if (!string.IsNullOrEmpty(EncryptedCVV))
            CVV = decryptFunc(EncryptedCVV);
    }
}
