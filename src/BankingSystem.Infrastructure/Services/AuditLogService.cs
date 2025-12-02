namespace BankingSystem.Infrastructure.Services;

using BankingSystem.Application.Interfaces;
using BankingSystem.Domain.Entities;
using BankingSystem.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class AuditLogService : IAuditLogService
{
    private readonly BankingSystemDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        BankingSystemDbContext context,
        ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAuditAsync(
        string entityName,
        string action,
        Guid? entityId,
        Guid? userId,
        object? oldValues = null,
        object? newValues = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                Entity = entityName,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Audit log created for {Entity} with action {Action}",
                entityName,
                action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log for {Entity}", entityName);
        }
    }
}
