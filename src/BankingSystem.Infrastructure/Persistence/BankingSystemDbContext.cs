namespace BankingSystem.Infrastructure.Persistence;

using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Models;
using BankingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

public class BankingSystemDbContext : DbContext, IApplicationDbContext
{
    public BankingSystemDbContext(DbContextOptions<BankingSystemDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<Bill> Bills { get; set; } = null!;
    public DbSet<Card> Cards { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
