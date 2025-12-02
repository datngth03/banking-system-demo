namespace BankingSystem.Application.Commands.Auth.Handlers;

using BankingSystem.Application.Commands.Auth;
using BankingSystem.Application.DTOs.Auth;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Constants;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.ValueObjects;
using BankingSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class RegisterHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterHandler(
        IApplicationDbContext context,
        IJwtService jwtService,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser != null)
            throw new ValidationFailureException(ValidationMessages.EmailAlreadyRegistered);

        // Create address if provided
        Address? address = null;
        if (!string.IsNullOrEmpty(request.Street) && !string.IsNullOrEmpty(request.City))
        {
            address = new Address(
                request.Street,
                request.City,
                request.State ?? string.Empty,
                request.PostalCode ?? string.Empty,
                request.Country ?? string.Empty);
        }

        // Create user with default User role
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            PhoneNumber = request.PhoneNumber,
            Address = address,
            DateOfBirth = request.DateOfBirth,
            Role = Role.User, // Explicitly set default role
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            LastLoginAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        // Generate tokens
        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        await _jwtService.CreateRefreshTokenAsync(user.Id, refreshToken);

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            UserId = user.Id,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}"
        };
    }
}
