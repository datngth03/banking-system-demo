namespace BankingSystem.Application.EventHandlers;

using MediatR;
using BankingSystem.Domain.DomainEvents;
using BankingSystem.Application.Interfaces;
using Microsoft.Extensions.Logging;

public class AccountCreatedEventHandler : INotificationHandler<AccountCreatedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<AccountCreatedEventHandler> _logger;

    public AccountCreatedEventHandler(
        INotificationService notificationService,
        ILogger<AccountCreatedEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(AccountCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {EventName} for account {AccountId}",
            nameof(AccountCreatedEvent),
            notification.AggregateId);

        try
        {
            await _notificationService.CreateNotificationAsync(
                notification.UserId,
                "Account Created",
                $"Your {notification.AccountType} account {notification.AccountNumber} has been successfully created.",
                "AccountCreated",
                cancellationToken);

            _logger.LogInformation("Notification sent for account {AccountId}", notification.AggregateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {EventName} for account {AccountId}",
                nameof(AccountCreatedEvent),
                notification.AggregateId);
            throw;
        }
    }
}
