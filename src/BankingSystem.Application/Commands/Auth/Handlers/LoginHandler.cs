namespace BankingSystem.Application.Commands.Auth.Handlers;

using MediatR;
using BankingSystem.Application.Commands.Auth;
using BankingSystem.Application.DTOs.Auth;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Constants;
using Microsoft.EntityFrameworkCore;

public class LoginHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _auditLogService;
    private readonly IMetricsService? _metrics;

    private const int MaxFailedAttempts = 5;
    private const int LockoutDurationMinutes = 15;

    public LoginHandler(
        IApplicationDbContext context,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        IAuditLogService auditLogService,
        IMetricsService? metrics = null)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _auditLogService = auditLogService;
        _metrics = metrics;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
            throw new UnauthorizedException(ValidationMessages.InvalidCredentials);

        // Check if account is locked (FIXED: Added parentheses)
        if (user.IsLockedOut())
        {
            var remainingMinutes = user.GetRemainingLockoutMinutes();
            throw new UnauthorizedException(
                $"Account is locked due to too many failed login attempts. Please try again in {remainingMinutes} minutes.");
        }

        if (!user.IsActive)
            throw new UnauthorizedException(ValidationMessages.UserAccountNotActive);

        // Verify password
        if (!_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            // Record failed login attempt with auto-lockout
            user.RecordFailedLogin(MaxFailedAttempts, LockoutDurationMinutes);
            await _context.SaveChangesAsync(cancellationToken);

            // Record metrics
            _metrics?.RecordAuthentication(false);

            // Audit log failed login
            await _auditLogService.LogAuditAsync(
                "User",
                "FailedLogin",
                user.Id,
                user.Id,
                null,
                new { Email = user.Email, FailedAttempts = user.FailedLoginAttempts },
                cancellationToken);

            // Check if account is now locked after this attempt
            if (user.IsLockedOut())
            {
                throw new UnauthorizedException(
                    $"Too many failed login attempts. Account is locked for {LockoutDurationMinutes} minutes.");
            }

            // Show remaining attempts if close to lockout
            var remainingAttempts = user.GetRemainingAttempts(MaxFailedAttempts);
            if (remainingAttempts > 0 && remainingAttempts <= 2)
            {
                throw new UnauthorizedException(
                    $"Invalid email or password. {remainingAttempts} attempts remaining before account lockout.");
            }

            throw new UnauthorizedException(ValidationMessages.InvalidCredentials);
        }

        // Record successful login (resets lockout counters)
        user.RecordSuccessfulLogin();
        await _context.SaveChangesAsync(cancellationToken);

        // Record metrics
        _metrics?.RecordAuthentication(true);

        // Audit log successful login
        await _auditLogService.LogAuditAsync(
            "User",
            "Login",
            user.Id,
            user.Id,
            null,
            new { Email = user.Email, LastLoginAt = user.LastSuccessfulLogin },
            cancellationToken);

        // Generate tokens
        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        await _jwtService.CreateRefreshTokenAsync(user.Id, refreshToken);

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60), // Should come from JwtSettings
            UserId = user.Id,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}"
        };
    }
}