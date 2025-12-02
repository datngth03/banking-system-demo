namespace BankingSystem.Infrastructure.Events;

using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Models;
using BankingSystem.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class EventPublisher : IEventPublisher
{
    private readonly BankingSystemDbContext _context;
    private readonly IPublisher _publisher;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(
        BankingSystemDbContext context,
        IPublisher publisher,
        ILogger<EventPublisher> logger)
    {
        _context = context;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        try
        {
            // Publish immediately using MediatR
            await _publisher.Publish(@event, cancellationToken);

            _logger.LogInformation("Event {EventType} published successfully", typeof(TEvent).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event {EventType}", typeof(TEvent).Name);
            throw;
        }
    }

    public async Task PublishAsync(params object[] events)
    {
        foreach (var @event in events)
        {
            try
            {
                var eventType = @event.GetType();
                var publishMethod = _publisher.GetType()
                    .GetMethods()
                    .First(m => m.Name == "Publish" && m.IsGenericMethodDefinition)
                    .MakeGenericMethod(eventType);

                var task = (Task)publishMethod.Invoke(_publisher, new object?[] { @event, CancellationToken.None })!;
                await task;

                _logger.LogInformation("Event {EventType} published successfully", eventType.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing event");
                throw;
            }
        }
    }
}
