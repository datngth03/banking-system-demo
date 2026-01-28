namespace BankingSystem.Infrastructure.Persistence.Configurations;

using BankingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.ReferenceNumber).HasMaxLength(50);
        builder.Property(t => t.TransactionType).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.TransactionDate).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();
        builder.Property(t => t.Status).HasMaxLength(50).IsRequired().HasDefaultValue("completed");

        // Stripe Payment Fields
        builder.Property(t => t.StripePaymentId).HasMaxLength(100);
        builder.Property(t => t.PaymentStatus);
        builder.Property(t => t.PaymentMethod).HasMaxLength(50).HasDefaultValue("Internal");
        builder.Property(t => t.ExternalReferenceId).HasMaxLength(100);
        builder.Property(t => t.Currency).HasMaxLength(3).HasDefaultValue("USD");
        builder.Property(t => t.Type).IsRequired();
        builder.Property(t => t.Reference).HasMaxLength(100);

        // Configure Amount value object
        builder.OwnsOne(t => t.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(18, 2).IsRequired();
            money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired().HasDefaultValue("USD");
        });

        // Configure BalanceAfter value object
        builder.OwnsOne(t => t.BalanceAfter, money =>
        {
            money.Property(m => m.Amount).HasColumnName("BalanceAfter").HasPrecision(18, 2).IsRequired();
            money.Property(m => m.Currency).HasColumnName("BalanceAfterCurrency").HasMaxLength(3).IsRequired().HasDefaultValue("USD");
        });

        // ============ INDEXES FOR PERFORMANCE ============

        // Unique index for reference number (lookups and reconciliation)
        builder.HasIndex(t => t.ReferenceNumber)
            .IsUnique()
            .HasDatabaseName("IX_Transactions_ReferenceNumber");

        // Index for account transactions (most common query)
        builder.HasIndex(t => t.AccountId)
            .HasDatabaseName("IX_Transactions_AccountId");

        // Index for transaction date (date range queries)
        builder.HasIndex(t => t.TransactionDate)
            .HasDatabaseName("IX_Transactions_TransactionDate")
            .IsDescending(); // Most recent first

        // Index for created date (audit queries)
        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("IX_Transactions_CreatedAt")
            .IsDescending();

        // Composite index for account + date range queries (GetTransactionHistory)
        builder.HasIndex(t => new { t.AccountId, t.TransactionDate })
            .HasDatabaseName("IX_Transactions_AccountId_TransactionDate")
            .IsDescending();

        // Composite index for account + transaction type (filtering)
        builder.HasIndex(t => new { t.AccountId, t.TransactionType })
            .HasDatabaseName("IX_Transactions_AccountId_TransactionType");

        // Index for transaction type (reporting)
        builder.HasIndex(t => t.TransactionType)
            .HasDatabaseName("IX_Transactions_TransactionType");

        // Composite index for account + type + date (advanced filtering)
        builder.HasIndex(t => new { t.AccountId, t.TransactionType, t.TransactionDate })
            .HasDatabaseName("IX_Transactions_AccountId_TransactionType_TransactionDate")
            .IsDescending();

        // Composite index for recent transactions by account (dashboard queries)
        builder.HasIndex(t => new { t.AccountId, t.CreatedAt })
            .HasDatabaseName("IX_Transactions_AccountId_CreatedAt")
            .IsDescending();

        // ============ INDEXES FOR STRIPE PAYMENT INTEGRATION ============

        // Index for Stripe payment ID (lookup by payment ID from webhooks)
        builder.HasIndex(t => t.StripePaymentId)
            .HasDatabaseName("IX_Transactions_StripePaymentId");

        // Index for external reference ID (webhook correlation)
        builder.HasIndex(t => t.ExternalReferenceId)
            .HasDatabaseName("IX_Transactions_ExternalReferenceId");

        // Index for payment status (query payments by status)
        builder.HasIndex(t => t.PaymentStatus)
            .HasDatabaseName("IX_Transactions_PaymentStatus");

        // Index for user ID + payment method (user card payment history)
        builder.HasIndex(t => new { t.UserId, t.PaymentMethod })
            .HasDatabaseName("IX_Transactions_UserId_PaymentMethod");

        // Composite index for pending payments (webhook processing)
        builder.HasIndex(t => new { t.PaymentStatus, t.CreatedAt })
            .HasDatabaseName("IX_Transactions_PaymentStatus_CreatedAt");
    }

}
