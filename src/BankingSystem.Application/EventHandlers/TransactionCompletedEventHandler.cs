namespace BankingSystem.Application.EventHandlers;

using MediatR;
using BankingSystem.Domain.DomainEvents;
using BankingSystem.Application.Interfaces;
using Microsoft.Extensions.Logging;

public class TransactionCompletedEventHandler : INotificationHandler<TransactionCompletedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<TransactionCompletedEventHandler> _logger;

    public TransactionCompletedEventHandler(
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<TransactionCompletedEventHandler> logger)
    {
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(TransactionCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {EventName} for transaction {TransactionId}",
            nameof(TransactionCompletedEvent),
            notification.AggregateId);

        try
        {
            // Create in-app notification
            await _notificationService.CreateNotificationAsync(
                notification.AccountId,
                "Transaction Completed",
                $"Your {notification.TransactionType} transaction of {notification.Amount} has been completed. {notification.Description}",
                "TransactionCompleted",
                cancellationToken);

            _logger.LogInformation("Notification created for transaction {TransactionId}",
                notification.AggregateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {EventName} for transaction {TransactionId}",
                nameof(TransactionCompletedEvent),
                notification.AggregateId);
            throw;
        }
    }
}
