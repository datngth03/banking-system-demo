namespace BankingSystem.Infrastructure.Persistence.Configurations;

using BankingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.HasKey(c => c.Id);

        // Encrypted fields (stored in database)
        builder.Property(c => c.EncryptedCardNumber)
            .HasColumnName("CardNumber")
            .HasMaxLength(256) // Encrypted data is longer
            .IsRequired();

        builder.Property(c => c.EncryptedCVV)
            .HasColumnName("CVV")
            .HasMaxLength(256) // Encrypted data is longer
            .IsRequired();

        // Plain text fields (ignored - not stored in database)
        builder.Ignore(c => c.CardNumber);
        builder.Ignore(c => c.CVV);
        builder.Ignore(c => c.MaskedCardNumber);
        builder.Ignore(c => c.MaskedCVV);

        // Other fields
        builder.Property(c => c.CardHolderName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Status).IsRequired();
        builder.Property(c => c.ExpiryDate).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.BlockedAt);
        builder.Property(c => c.BlockedReason).HasMaxLength(256);

        // ============ INDEXES FOR PERFORMANCE ============

        // Unique index on encrypted card number (for uniqueness check)
        builder.HasIndex(c => c.EncryptedCardNumber)
            .IsUnique()
            .HasDatabaseName("IX_Cards_EncryptedCardNumber");

        // Index for user cards lookup
        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("IX_Cards_UserId");

        // Index for account cards lookup
        builder.HasIndex(c => c.AccountId)
            .HasDatabaseName("IX_Cards_AccountId");

        // Index for card status filtering
        builder.HasIndex(c => c.Status)
            .HasDatabaseName("IX_Cards_Status");

        // Composite index for user cards by status (GetMyCards query)
        builder.HasIndex(c => new { c.UserId, c.Status })
            .HasDatabaseName("IX_Cards_UserId_Status");

        // Composite index for account cards by status
        builder.HasIndex(c => new { c.AccountId, c.Status })
            .HasDatabaseName("IX_Cards_AccountId_Status");

        // Index for expiry date (expiring cards report)
        builder.HasIndex(c => c.ExpiryDate)
            .HasDatabaseName("IX_Cards_ExpiryDate");

        // Composite index for active/blocked cards by user
        builder.HasIndex(c => new { c.UserId, c.Status, c.ExpiryDate })
            .HasDatabaseName("IX_Cards_UserId_Status_ExpiryDate");

        // Index for blocked cards (security monitoring)
        builder.HasIndex(c => c.BlockedAt)
            .HasDatabaseName("IX_Cards_BlockedAt")
            .HasFilter("\"BlockedAt\" IS NOT NULL");
    }
}
