using BankingSystem.Domain.Interfaces;

namespace BankingSystem.Domain.Entities;

public class Notification : IEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
}
