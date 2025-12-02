namespace BankingSystem.Infrastructure.Persistence.Configurations;

using BankingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AccountNumber).HasMaxLength(50).IsRequired();
        builder.Property(a => a.IBAN).HasMaxLength(34);
        builder.Property(a => a.BIC).HasMaxLength(11);
        builder.Property(a => a.AccountType).IsRequired();
        builder.Property(a => a.IsActive).HasDefaultValue(true);
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.ClosedAt);

        builder.OwnsOne(a => a.Balance, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Balance").HasPrecision(18, 2).IsRequired();
            money.Property(m => m.Currency).HasMaxLength(3).IsRequired().HasDefaultValue("USD");
        });

        builder.HasMany(a => a.Transactions)
            .WithOne(t => t.Account)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Bills)
            .WithOne(b => b.Account)
            .HasForeignKey(b => b.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // ============ INDEXES FOR PERFORMANCE ============
        
        // Unique index for account number (lookups by account number)
        builder.HasIndex(a => a.AccountNumber)
            .IsUnique()
            .HasDatabaseName("IX_Accounts_AccountNumber");
        
        // Index for user account lookups (most common query)
        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_Accounts_UserId");
        
        // Composite index for active accounts by user (GetMyAccounts query)
        builder.HasIndex(a => new { a.UserId, a.IsActive })
            .HasDatabaseName("IX_Accounts_UserId_IsActive");
        
        // Index for account type filtering
        builder.HasIndex(a => a.AccountType)
            .HasDatabaseName("IX_Accounts_AccountType");
        
        // Index for active status queries
        builder.HasIndex(a => a.IsActive)
            .HasDatabaseName("IX_Accounts_IsActive");

        // Composite index for account type + active status
        builder.HasIndex(a => new { a.AccountType, a.IsActive })
            .HasDatabaseName("IX_Accounts_AccountType_IsActive");

        // Index for IBAN lookups (international transfers)
        builder.HasIndex(a => a.IBAN)
            .HasDatabaseName("IX_Accounts_IBAN")
            .HasFilter("\"IBAN\" IS NOT NULL");

        // Index for created date (reporting queries)
        builder.HasIndex(a => a.CreatedAt)
            .HasDatabaseName("IX_Accounts_CreatedAt");

        // Composite index for user + account type (common filter)
        builder.HasIndex(a => new { a.UserId, a.AccountType, a.IsActive })
            .HasDatabaseName("IX_Accounts_UserId_AccountType_IsActive");
    }
}
