namespace BankingSystem.Infrastructure.Persistence.Configurations;

using BankingSystem.Application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(om => om.Id);

        builder.Property(om => om.EventType).HasMaxLength(500).IsRequired();
        builder.Property(om => om.EventData).HasColumnType("text").IsRequired();
        builder.Property(om => om.IsProcessed).HasDefaultValue(false);
        builder.Property(om => om.CreatedAt).IsRequired();
        builder.Property(om => om.ProcessedAt);
        builder.Property(om => om.Error).HasColumnType("text");

        builder.HasIndex(om => new { om.IsProcessed, om.CreatedAt });
    }
}
