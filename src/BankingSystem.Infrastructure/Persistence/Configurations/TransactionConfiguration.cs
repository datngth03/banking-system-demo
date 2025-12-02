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
    }
}
