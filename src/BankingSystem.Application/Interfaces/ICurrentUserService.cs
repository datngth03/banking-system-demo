namespace BankingSystem.Application.Interfaces;

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
}
