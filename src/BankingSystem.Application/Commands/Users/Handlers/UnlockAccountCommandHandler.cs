using BankingSystem.Application.Commands.Users;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankingSystem.Application.Commands.Users.Handlers;

/// <summary>
/// Handler for unlocking user accounts
/// </summary>
public class UnlockAccountCommandHandler : IRequestHandler<UnlockAccountCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UnlockAccountCommandHandler> _logger;

    public UnlockAccountCommandHandler(
        IApplicationDbContext context,
        IAuditLogService auditLogService,
        ILogger<UnlockAccountCommandHandler> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Unit> Handle(UnlockAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Attempt to unlock non-existent user {UserId}", request.UserId);
            throw new NotFoundException($"User with ID {request.UserId} not found");
        }

        if (!user.IsLockedOut())
        {
            _logger.LogInformation("User {UserId} is not locked, no action needed", request.UserId);
            return Unit.Value;
        }

        // Unlock the account
        user.UnlockAccount();
        await _context.SaveChangesAsync(cancellationToken);

        // Audit log the unlock action
        await _auditLogService.LogAuditAsync(
            entityName: "User",
            action: "AccountUnlocked",
            entityId: user.Id,
            userId: request.AdminUserId,
            oldValues: new { LockoutEnd = user.LockoutEnd, FailedLoginAttempts = user.FailedLoginAttempts },
            newValues: new { LockoutEnd = (DateTime?)null, FailedLoginAttempts = 0 },
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "User {UserId} account unlocked by admin {AdminUserId}",
            request.UserId,
            request.AdminUserId);

        return Unit.Value;
    }
}
