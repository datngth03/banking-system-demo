namespace BankingSystem.Infrastructure.BackgroundJobs;

using BankingSystem.Application.Interfaces;
using Microsoft.Extensions.Logging;

public class OutboxPublisherJob
{
    private readonly IOutboxService _outboxService;
    private readonly ILogger<OutboxPublisherJob> _logger;

    public OutboxPublisherJob(
        IOutboxService outboxService,
        ILogger<OutboxPublisherJob> logger)
    {
        _outboxService = outboxService;
        _logger = logger;
    }

    public async Task PublishOutboxMessages()
    {
        _logger.LogInformation("Starting outbox message publishing");

        try
        {
            await _outboxService.PublishOutboxMessagesAsync();
            _logger.LogInformation("Outbox messages published successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing outbox messages");
            throw;
        }
    }
}
