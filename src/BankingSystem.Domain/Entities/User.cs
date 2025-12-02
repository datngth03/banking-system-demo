using BankingSystem.Domain.Interfaces;
using BankingSystem.Domain.ValueObjects;
using BankingSystem.Domain.Enums;

namespace BankingSystem.Domain.Entities;

public class User : IEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public Address? Address { get; set; }
    public DateTime DateOfBirth { get; set; }
    public Role Role { get; set; } = Role.User;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    
    // Account lockout properties
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; }
    public DateTime? LastLoginAttempt { get; set; }
    public DateTime? LastSuccessfulLogin { get; set; }

    // Navigation properties
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<Card> Cards { get; set; } = new List<Card>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    // Helper methods for account lockout management
    
    /// <summary>
    /// Checks if the account is currently locked out
    /// </summary>
    public bool IsLockedOut()
    {
        return LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// Records a failed login attempt and locks the account if threshold is exceeded
    /// </summary>
    /// <param name="maxFailedAttempts">Maximum allowed failed attempts before lockout (default: 5)</param>
    /// <param name="lockoutMinutes">Duration of lockout in minutes (default: 15)</param>
    public void RecordFailedLogin(int maxFailedAttempts = 5, int lockoutMinutes = 15)
    {
        LastLoginAttempt = DateTime.UtcNow;
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= maxFailedAttempts)
        {
            LockoutEnd = DateTime.UtcNow.AddMinutes(lockoutMinutes);
        }
    }

    /// <summary>
    /// Records a successful login and resets lockout counters
    /// </summary>
    public void RecordSuccessfulLogin()
    {
        LastSuccessfulLogin = DateTime.UtcNow;
        LastLoginAt = DateTime.UtcNow;
        LastLoginAttempt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockoutEnd = null;
    }

    /// <summary>
    /// Manually unlocks the account (admin action)
    /// </summary>
    public void UnlockAccount()
    {
        FailedLoginAttempts = 0;
        LockoutEnd = null;
    }

    /// <summary>
    /// Gets the remaining lockout time in minutes
    /// </summary>
    public double GetRemainingLockoutMinutes()
    {
        if (!IsLockedOut())
            return 0;

        var remaining = LockoutEnd!.Value - DateTime.UtcNow;
        return Math.Ceiling(remaining.TotalMinutes);
    }

    /// <summary>
    /// Gets the number of remaining login attempts before lockout
    /// </summary>
    public int GetRemainingAttempts(int maxFailedAttempts = 5)
    {
        return Math.Max(0, maxFailedAttempts - FailedLoginAttempts);
    }
}
