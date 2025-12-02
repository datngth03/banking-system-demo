namespace BankingSystem.Infrastructure.Persistence.Configurations;

using BankingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(al => al.Id);

        builder.Property(al => al.Entity).HasMaxLength(200).IsRequired();
        builder.Property(al => al.Action).HasMaxLength(50).IsRequired();
        builder.Property(al => al.UserId);
        builder.Property(al => al.OldValues).HasColumnType("text");
        builder.Property(al => al.NewValues).HasColumnType("text");
        builder.Property(al => al.CreatedAt).IsRequired();
        builder.Property(al => al.IpAddress).HasMaxLength(100);
        builder.Property(al => al.UserAgent).HasMaxLength(256);

        builder.HasIndex(al => al.Entity);
        builder.HasIndex(al => al.UserId);
        builder.HasIndex(al => al.CreatedAt);
    }
}
