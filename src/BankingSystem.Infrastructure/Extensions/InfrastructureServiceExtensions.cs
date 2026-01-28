namespace BankingSystem.Infrastructure.Extensions;

using BankingSystem.Application.Interfaces;
using BankingSystem.Domain.Interfaces;
using BankingSystem.Infrastructure.BackgroundJobs;
using BankingSystem.Infrastructure.Events;
using BankingSystem.Infrastructure.Persistence;
using BankingSystem.Infrastructure.Services;
using BankingSystem.Infrastructure.Repositories;
using BankingSystem.Infrastructure.Monitoring;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<BankingSystemDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection not configured");

            options.UseNpgsql(connectionString);
        });

        // HTTP Context
        services.AddHttpContextAccessor();

        // Distributed Cache (Redis)
        services.AddStackExchangeRedisCache(options =>
        {
            var connectionString = configuration.GetConnectionString("Redis")
                ?? "localhost:6379";
            options.Configuration = connectionString;
        });

        // UnitOfWork & Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

        // Core Services
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<BankingSystemDbContext>());
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IDataEncryptionService, DataEncryptionService>();

        // Domain Services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IEventPublisher, EventPublisher>();

        // Additional Services
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IInterestCalculationService, InterestCalculationService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IErrorTrackingService, ErrorTrackingService>();

        // Stripe Payment Service
        var stripeSettings = configuration.GetSection("StripeSettings");
        services.Configure<BankingSystem.Application.Models.StripeSettings>(stripeSettings);
        services.AddScoped<IPaymentService, StripePaymentService>();

        // Monitoring & Metrics
        services.AddSingleton<BankingSystemMetrics>();
        services.AddScoped<MetricsCollectorService>();
        services.AddScoped<MetricsService>();

        // Background Jobs
        services.AddScoped<OutboxPublisherJob>();
        services.AddScoped<InterestApplicationJob>();
        services.AddScoped<IBackgroundJobScheduler, BackgroundJobScheduler>();

        // Hangfire Configuration
        services.AddHangfire(config =>
        {
            var connectionString = configuration.GetConnectionString("HangfireConnection")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not configured");

            config.UsePostgreSqlStorage(connectionString);
        });

        services.AddHangfireServer();

        return services;
    }
}
