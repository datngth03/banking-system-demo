using BankingSystem.Domain.Interfaces;

namespace BankingSystem.Domain.Entities;

public class RefreshToken : IEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
}
