namespace BankingSystem.Application.Interfaces;

/// <summary>
/// Interface for audit logging operations
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Logs an audit event
    /// </summary>
    Task LogAuditAsync(
        string entityName,
        string action,
        Guid? entityId,
        Guid? userId,
        object? oldValues = null,
        object? newValues = null,
        CancellationToken cancellationToken = default);
}