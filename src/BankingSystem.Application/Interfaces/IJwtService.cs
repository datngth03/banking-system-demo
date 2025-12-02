namespace BankingSystem.Application.Interfaces;

using BankingSystem.Domain.Entities;
using System.Security.Claims;

public interface IJwtService
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, string token);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token);
    Task<bool> ValidateRefreshTokenAsync(string token);
}
