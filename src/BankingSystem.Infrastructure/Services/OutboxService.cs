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
            .Take(100) // Process in batches to avoid memory issues
            .ToListAsync(cancellationToken);

        if (!messages.Any())
        {
            _logger.LogDebug("No unpublished outbox messages found");
            return;
        }

        _logger.LogInformation("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                // Try to resolve the full type name with namespace
                var eventType = ResolveEventType(message.EventType);
                
                if (eventType == null)
                {
                    _logger.LogWarning(
                        "Event type '{EventType}' not found for message {MessageId}. Marking as processed with error.",
                        message.EventType,
                        message.Id);
                    
                    message.IsProcessed = true;
                    message.ProcessedAt = DateTime.UtcNow;
                    message.Error = $"Event type '{message.EventType}' not found";
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message.EventData, eventType);
                
                if (@event != null)
                {
                    await _publisher.Publish(@event, cancellationToken);
                    
                    message.IsProcessed = true;
                    message.ProcessedAt = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Outbox message {MessageId} with type {EventType} published successfully",
                        message.Id,
                        message.EventType);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to deserialize event data for message {MessageId}",
                        message.Id);
                    
                    message.Error = "Deserialization returned null";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error publishing outbox message {MessageId} with type {EventType}",
                    message.Id,
                    message.EventType);
                
                // Store error but don't mark as processed - will retry later
                message.Error = ex.Message;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Resolves event type by checking common namespaces.
    /// Supports both simple names (BillPaymentCompletedEvent) and fully qualified names.
    /// </summary>
    private Type? ResolveEventType(string eventTypeName)
    {
        // If it's already a fully qualified name, try to get it directly
        var type = Type.GetType(eventTypeName);
        if (type != null)
            return type;

        // Common namespaces to search for event types
        var namespaces = new[]
        {
            "BankingSystem.Application.Events",
            "BankingSystem.Domain.DomainEvents",
            "BankingSystem.Application.Commands",
            "BankingSystem.Application.Models"
        };

        foreach (var ns in namespaces)
        {
            var fullTypeName = $"{ns}.{eventTypeName}, BankingSystem.Application";
            type = Type.GetType(fullTypeName);
            if (type != null)
            {
                _logger.LogDebug("Resolved event type '{EventType}' to {FullType}", 
                    eventTypeName, fullTypeName);
                return type;
            }

            // Also try Domain assembly
            fullTypeName = $"{ns}.{eventTypeName}, BankingSystem.Domain";
            type = Type.GetType(fullTypeName);
            if (type != null)
            {
                _logger.LogDebug("Resolved event type '{EventType}' to {FullType}", 
                    eventTypeName, fullTypeName);
                return type;
            }
        }

        _logger.LogWarning("Could not resolve event type '{EventType}' in any known namespace", 
            eventTypeName);
        return null;
    }
}
