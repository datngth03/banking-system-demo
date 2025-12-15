namespace BankingSystem.Infrastructure.Extensions;

using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Models;
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
using Microsoft.Extensions.Hosting;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Configuration Settings
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.Configure<InterestSettings>(configuration.GetSection("InterestSettings"));

        // HTTP Context Accessor for current user service
        services.AddHttpContextAccessor();

        // Database
        services.AddDbContext<BankingSystemDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection not configured");

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            });

            // Enable query tracking optimization
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
        });

        // UnitOfWork & Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

        // Services
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<BankingSystemDbContext>());
        
        // Email Service - Use Mock in Development if SMTP not configured
        if (environment.IsDevelopment())
        {
            var emailSettings = configuration.GetSection("EmailSettings").Get<EmailSettings>();
            if (string.IsNullOrEmpty(emailSettings?.Username) && string.IsNullOrEmpty(emailSettings?.Password))
            {
                // Use Mock Email Service when SMTP credentials are not configured
                services.AddScoped<IEmailService, MockEmailService>();
            }
            else
            {
                services.AddScoped<IEmailService, EmailService>();
            }
        }
        else
        {
            services.AddScoped<IEmailService, EmailService>();
        }
        
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IInterestCalculationService, InterestCalculationService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IEventPublisher, EventPublisher>();

        // Monitoring & Metrics
        services.AddSingleton<IMetricsService, MetricsService>();
        services.AddSingleton<BankingSystemMetrics>();
        services.AddHostedService<MetricsCollectorService>();

        // Error Tracking
        services.AddScoped<IErrorTrackingService, ErrorTrackingService>();

        // Data Encryption Service
        services.AddScoped<IDataEncryptionService, DataEncryptionService>();

        // Caching - Redis Distributed Cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis")
                ?? "localhost:6379";
        });

        // Cache Service (wrapper around IDistributedCache)
        services.AddScoped<ICacheService, CacheService>();

        // Background Jobs
        services.AddScoped<OutboxPublisherJob>();
        services.AddScoped<InterestApplicationJob>();
        services.AddScoped<IBackgroundJobScheduler, BackgroundJobScheduler>();

        // Hangfire (skip for in-memory/testing scenarios)
        var connectionString = configuration.GetConnectionString("HangfireConnection")
            ?? configuration.GetConnectionString("DefaultConnection");

        if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("InMemoryDatabase"))
        {
            services.AddHangfire(config =>
            {
                config.UsePostgreSqlStorage(connectionString);
            });

            services.AddHangfireServer();
        }

        return services;
    }
}
