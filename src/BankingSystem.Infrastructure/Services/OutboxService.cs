namespace BankingSystem.Infrastructure.Services;

using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Models;
using BankingSystem.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class OutboxService : IOutboxService
{
    private readonly BankingSystemDbContext _context;
    private readonly IPublisher _publisher;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(
        BankingSystemDbContext context,
        IPublisher publisher,
        ILogger<OutboxService> logger)
    {
        _context = context;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task PublishOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        var messages = await _context.OutboxMessages
            .Where(m => !m.IsProcessed)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        if (!messages.Any())
        {
            _logger.LogDebug("No unpublished outbox messages found");
            return;
        }

        foreach (var message in messages)
        {
            try
            {
                // Deserialize and publish the event
                var eventType = Type.GetType(message.EventType);
                if (eventType == null)
                {
                    _logger.LogWarning("Event type {EventType} not found", message.EventType);
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message.EventData, eventType);
                if (@event != null)
                {
                    await _publisher.Publish(@event, cancellationToken);
                }

                message.IsProcessed = true;
                message.ProcessedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Outbox message {MessageId} with type {EventType} published successfully",
                    message.Id,
                    message.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error publishing outbox message {MessageId}",
                    message.Id);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
