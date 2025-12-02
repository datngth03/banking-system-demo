namespace BankingSystem.Application.Commands.Auth.Handlers;

using BankingSystem.Application.Commands.Auth;
using BankingSystem.Application.DTOs.Auth;
using BankingSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;

    public RefreshTokenHandler(
        IApplicationDbContext context,
        IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Validate the access token (even if expired)
        var principal = _jwtService.GetPrincipalFromExpiredToken(request.Token);
        if (principal == null)
            throw new UnauthorizedAccessException("Invalid token");

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? principal.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid token claims");

        // Validate the refresh token
        if (!await _jwtService.ValidateRefreshTokenAsync(request.RefreshToken))
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        // Get user
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("User not found or inactive");

        // Revoke old refresh token
        await _jwtService.RevokeRefreshTokenAsync(request.RefreshToken);

        // Generate new tokens
        var newToken = _jwtService.GenerateToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        await _jwtService.CreateRefreshTokenAsync(user.Id, newRefreshToken);

        return new AuthResponseDto
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            UserId = user.Id,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}"
        };
    }
}
