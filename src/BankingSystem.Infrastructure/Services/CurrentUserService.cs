namespace BankingSystem.Infrastructure.Services;

using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

/// <summary>
/// Implementation of ICurrentUserService that retrieves user information from HTTP context
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CurrentUserService> _logger;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<CurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Guid? UserId
    {
        get
        {
            // Try multiple claim types for UserId
            var userIdClaim = GetClaim(ClaimTypes.NameIdentifier)
                ?? GetClaim(JwtRegisteredClaimNames.Sub)
                ?? GetClaim("sub");

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            _logger.LogDebug("Could not parse UserId claim: {Claim}", userIdClaim);
            return null;
        }
    }

    public string? Email
    {
        get
        {
            // Try multiple claim types for Email
            return GetClaim(ClaimTypes.Email)
                ?? GetClaim("email")
                ?? GetClaim(JwtRegisteredClaimNames.Email);
        }
    }

    public string? Role => GetClaim(ClaimTypes.Role);

    public string? FullName
    {
        get
        {
            // Try multiple claim types for FullName
            return GetClaim("full_name")
                ?? GetClaim(ClaimTypes.Name)
                ?? GetClaim(JwtRegisteredClaimNames.Name);
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool IsStaff => IsInRole(Roles.Admin) || IsInRole(Roles.Manager) || IsInRole(Roles.Support);

    public bool IsAdmin => IsInRole(Roles.Admin);

    public IEnumerable<Claim> GetClaims()
    {
        return _httpContextAccessor.HttpContext?.User?.Claims ?? Enumerable.Empty<Claim>();
    }

    public string? GetClaim(string claimType)
    {
        if (string.IsNullOrWhiteSpace(claimType))
        {
            return null;
        }

        var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(claimType);
        return claim?.Value;
    }

    public bool IsInRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
    }
}
