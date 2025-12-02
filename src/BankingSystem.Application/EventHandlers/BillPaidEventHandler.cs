namespace BankingSystem.Application.EventHandlers;

using MediatR;
using BankingSystem.Application.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handler for Bill Paid domain event
/// This is a placeholder - implement based on your domain events
/// </summary>
public class BillPaidEventHandler : INotificationHandler<BillPaidEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<BillPaidEventHandler> _logger;

    public BillPaidEventHandler(
        INotificationService notificationService,
        ILogger<BillPaidEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(BillPaidEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {EventName} for bill {BillId}",
            nameof(BillPaidEvent),
            notification.AggregateId);

        try
        {
            await _notificationService.CreateNotificationAsync(
                notification.UserId,
                "Bill Paid",
                $"Your bill payment has been successfully processed.",
                "BillPaid",
                cancellationToken);

            _logger.LogInformation("Notification sent for bill {BillId}", 
                notification.AggregateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {EventName} for bill {BillId}",
                nameof(BillPaidEvent),
                notification.AggregateId);
            throw;
        }
    }
}

/// <summary>
/// Placeholder for BillPaidEvent domain event
/// </summary>
public class BillPaidEvent : INotification
{
    public Guid AggregateId { get; set; }
    public Guid UserId { get; set; }
    public DateTime OccurredOn { get; set; }
}
