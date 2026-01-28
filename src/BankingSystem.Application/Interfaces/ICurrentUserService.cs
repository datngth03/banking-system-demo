namespace BankingSystem.Application.Interfaces;

using System.Security.Claims;

/// <summary>
/// Service to get current authenticated user information from HTTP context
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the current user's email
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets the current user's role
    /// </summary>
    string? Role { get; }

    /// <summary>
    /// Gets the current user's full name
    /// </summary>
    string? FullName { get; }

    /// <summary>
    /// Gets all claims for the current user
    /// </summary>
    IEnumerable<Claim> GetClaims();

    /// <summary>
    /// Gets a specific claim value by type
    /// </summary>
    string? GetClaim(string claimType);

    /// <summary>
    /// Checks if the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Checks if the current user is in a specific role
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// Checks if the current user is staff (Admin, Manager, or Support)
    /// </summary>
    bool IsStaff { get; }

    /// <summary>
    /// Checks if the user has admin role
    /// </summary>
    bool IsAdmin { get; }
}
