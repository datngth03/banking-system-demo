namespace BankingSystem.Infrastructure.Services;

using BankingSystem.Application.Interfaces;
using BankingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

public class UserService : IUserService
{
    private readonly BankingSystemDbContext _context;
    private readonly ILogger<UserService> _logger;
    private readonly IDistributedCache _cache;

    public UserService(
        BankingSystemDbContext context,
        ILogger<UserService> logger,
        IDistributedCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
    }

    public async Task<bool> UserExistsAsync(Guid userId)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<dynamic?> GetUserByIdAsync(Guid userId)
    {
        var cacheKey = $"user:{userId}";

        // Try to get from cache
        var cachedUser = await _cache.GetStringAsync(cacheKey);
        if (cachedUser != null)
        {
            _logger.LogInformation("User {UserId} retrieved from cache", userId);
            return JsonSerializer.Deserialize<dynamic>(cachedUser);
        }

        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.PhoneNumber,
                u.DateOfBirth,
                u.IsActive,
                u.CreatedAt,
                Address = u.Address != null ? new
                {
                    u.Address.Street,
                    u.Address.City,
                    u.Address.State,
                    u.Address.PostalCode,
                    u.Address.Country
                } : null
            })
            .FirstOrDefaultAsync();

        if (user != null)
        {
            // Cache for 10 minutes
            var serializedUser = JsonSerializer.Serialize(user);
            await _cache.SetStringAsync(cacheKey, serializedUser, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
            _logger.LogInformation("User {UserId} cached", userId);
        }

        return user;
    }

    public async Task<dynamic?> GetUserByEmailAsync(string email)
    {
        var user = await _context.Users
            .Where(u => u.Email == email)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.PhoneNumber,
                u.DateOfBirth,
                u.IsActive,
                u.CreatedAt
            })
            .FirstOrDefaultAsync();

        return user;
    }
}
