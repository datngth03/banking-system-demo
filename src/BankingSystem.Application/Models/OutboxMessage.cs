namespace BankingSystem.Application.Models;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public bool IsProcessed { get; set; }
    public string? Error { get; set; }

    public static OutboxMessage Create(string eventType, string eventData)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            EventData = eventData,
            CreatedAt = DateTime.UtcNow,
            IsProcessed = false
        };
    }
}
