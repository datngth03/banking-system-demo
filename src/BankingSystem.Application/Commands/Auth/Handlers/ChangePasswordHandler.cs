namespace BankingSystem.Application.Commands.Auth.Handlers;

using BankingSystem.Application.Commands.Auth;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ChangePasswordHandler> _logger;

    public ChangePasswordHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<ChangePasswordHandler> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        // Get user
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found", request.UserId);
            throw new NotFoundException(string.Format(ValidationMessages.UserNotFound, request.UserId));
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Inactive user {UserId} attempted to change password", request.UserId);
            throw new UnauthorizedException(ValidationMessages.UserAccountNotActive);
        }

        // Verify current password
        if (!_passwordHasher.VerifyPassword(user.PasswordHash, request.CurrentPassword))
        {
            _logger.LogWarning("User {UserId} provided incorrect current password", request.UserId);
            throw new UnauthorizedException(ValidationMessages.CurrentPasswordIncorrect);
        }

        // Verify new password and confirm password match
        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new ValidationFailureException(ValidationMessages.PasswordMismatch);
        }

        // Check if new password is same as current password
        if (_passwordHasher.VerifyPassword(user.PasswordHash, request.NewPassword))
        {
            throw new ValidationFailureException(ValidationMessages.NewPasswordSameAsCurrent);
        }

        // Hash and update password
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} successfully changed password", request.UserId);

        return Unit.Value;
    }
}
