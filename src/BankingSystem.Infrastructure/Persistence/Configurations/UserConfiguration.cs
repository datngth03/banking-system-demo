namespace BankingSystem.Infrastructure.Persistence.Configurations;

using BankingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.Property(u => u.Role).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt);
        builder.Property(u => u.LastLoginAt);

        // Account lockout properties
        builder.Property(u => u.FailedLoginAttempts).HasDefaultValue(0);
        builder.Property(u => u.LockoutEnd);
        builder.Property(u => u.LastLoginAttempt);
        builder.Property(u => u.LastSuccessfulLogin);

        // Configure Address as owned entity
        builder.OwnsOne(u => u.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("Street").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
            address.Property(a => a.State).HasColumnName("State").HasMaxLength(100);
            address.Property(a => a.PostalCode).HasColumnName("PostalCode").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("Country").HasMaxLength(100);
        });

        builder.HasMany(u => u.Accounts)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Cards)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Notifications)
            .WithOne(n => n.User)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ============ INDEXES FOR PERFORMANCE ============

        // Unique index for email (login queries)
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        // Index for phone number lookups
        builder.HasIndex(u => u.PhoneNumber)
            .HasDatabaseName("IX_Users_PhoneNumber");

        // Index for role-based queries
        builder.HasIndex(u => u.Role)
            .HasDatabaseName("IX_Users_Role");

        // Index for active status queries
        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");

        // Composite index for active users by role (common query pattern)
        builder.HasIndex(u => new { u.IsActive, u.Role })
            .HasDatabaseName("IX_Users_IsActive_Role");

        // Index for lockout queries (check if user is locked)
        builder.HasIndex(u => u.LockoutEnd)
            .HasDatabaseName("IX_Users_LockoutEnd")
            .HasFilter("\"LockoutEnd\" IS NOT NULL");

        // Index for login analytics (last login tracking)
        builder.HasIndex(u => u.LastSuccessfulLogin)
            .HasDatabaseName("IX_Users_LastSuccessfulLogin");

        // Composite index for finding locked accounts
        builder.HasIndex(u => new { u.IsActive, u.LockoutEnd })
            .HasDatabaseName("IX_Users_IsActive_LockoutEnd");
    }
}
