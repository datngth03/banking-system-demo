using BankingSystem.Domain.Enums;
using BankingSystem.Domain.Interfaces;
using BankingSystem.Domain.ValueObjects;

namespace BankingSystem.Domain.Entities;

public class Transaction : IEntity
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid? RelatedAccountId { get; set; }
    public Guid? UserId { get; set; } // For direct card charges not tied to account
    public TransactionType TransactionType { get; set; }
    public Money Amount { get; set; } = new Money(0, "USD");
    public Money BalanceAfter { get; set; } = new Money(0, "USD");
    public string Description { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Status { get; set; } = "completed"; // completed, pending, failed, refunded

    // Stripe Payment Integration Fields
    /// <summary>
    /// Stripe Payment ID (charge ID or payment intent ID)
    /// </summary>
    public string? StripePaymentId { get; set; }

    /// <summary>
    /// Payment status from Stripe (Pending, Processing, Succeeded, Failed, Refunded, Disputed)
    /// </summary>
    public PaymentStatus? PaymentStatus { get; set; }

    /// <summary>
    /// Payment method (Internal, Card, ACH, WireTransfer, etc.)
    /// </summary>
    public string? PaymentMethod { get; set; } = "Internal";

    /// <summary>
    /// External reference for webhook correlation
    /// </summary>
    public string? ExternalReferenceId { get; set; }

    /// <summary>
    /// Currency code for payments (e.g., USD, EUR)
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Type of transaction (Deposit, Withdrawal, Transfer, CardCharge, BillPayment, etc.)
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Reference for transaction tracking
    /// </summary>
    public string Reference { get; set; } = string.Empty;

    // Navigation properties
    public Account? Account { get; set; }
    public Account? RelatedAccount { get; set; }
    public User? User { get; set; }
}
