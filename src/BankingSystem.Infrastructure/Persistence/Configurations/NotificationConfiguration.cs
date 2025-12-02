namespace BankingSystem.Infrastructure.Persistence.Configurations;

using BankingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.Type).HasMaxLength(50);
        builder.Property(n => n.IsRead).HasDefaultValue(false);
        builder.Property(n => n.CreatedAt).IsRequired();
        builder.Property(n => n.ReadAt);

        // ============ INDEXES FOR PERFORMANCE ============

        // Index for user notifications
        builder.HasIndex(n => n.UserId)
            .HasDatabaseName("IX_Notifications_UserId");

        // Composite index for unread notifications (GetUnreadNotifications query)
        builder.HasIndex(n => new { n.UserId, n.IsRead })
            .HasDatabaseName("IX_Notifications_UserId_IsRead");

        // Index for notification type filtering
        builder.HasIndex(n => n.Type)
            .HasDatabaseName("IX_Notifications_Type");

        // Index for created date (recent notifications)
        builder.HasIndex(n => n.CreatedAt)
            .HasDatabaseName("IX_Notifications_CreatedAt")
            .IsDescending();

        // Composite index for recent unread notifications
        builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_UserId_IsRead_CreatedAt")
            .IsDescending();

        // Index for read notifications (analytics)
        builder.HasIndex(n => n.ReadAt)
            .HasDatabaseName("IX_Notifications_ReadAt")
            .HasFilter("\"ReadAt\" IS NOT NULL");
    }
}
