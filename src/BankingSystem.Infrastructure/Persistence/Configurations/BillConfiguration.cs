namespace BankingSystem.Infrastructure.Persistence.Configurations;

using BankingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BillNumber).HasMaxLength(50).IsRequired();
        builder.Property(b => b.Biller).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Description).HasMaxLength(500);
        builder.Property(b => b.IsPaid).HasDefaultValue(false);
        builder.Property(b => b.CreatedAt).IsRequired();

        builder.OwnsOne(b => b.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(18, 2).IsRequired();
            money.Property(m => m.Currency).HasMaxLength(3).IsRequired();
        });

        builder.HasIndex(b => b.BillNumber).IsUnique();
        builder.HasIndex(b => b.AccountId);
        builder.HasIndex(b => b.IsPaid);
    }
}
