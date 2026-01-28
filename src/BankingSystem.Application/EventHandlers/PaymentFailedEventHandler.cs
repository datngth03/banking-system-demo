namespace BankingSystem.Application.EventHandlers;

using BankingSystem.Application.Events;
using BankingSystem.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles PaymentFailedEvent to send failure notifications
/// </summary>
public class PaymentFailedEventHandler : INotificationHandler<PaymentFailedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<PaymentFailedEventHandler> _logger;

    public PaymentFailedEventHandler(
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<PaymentFailedEventHandler> logger)
    {
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(PaymentFailedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Processing payment failure notification for Transaction {TransactionId}, User {UserId}, Reason: {Reason}",
            notification.TransactionId,
            notification.UserId,
            notification.FailureReason);

        try
        {
            // Create in-app notification
            await CreateNotificationAsync(notification, cancellationToken);

            // Send email alert
            if (!string.IsNullOrEmpty(notification.UserEmail))
            {
                await SendEmailNotificationAsync(notification, cancellationToken);
            }

            _logger.LogInformation(
                "Successfully processed payment failure notification for Transaction {TransactionId}",
                notification.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing payment failure notification for Transaction {TransactionId}",
                notification.TransactionId);
        }
    }

    private async Task CreateNotificationAsync(
        PaymentFailedEvent notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = $"Payment Failed: {notification.Currency} {notification.Amount / 100.0:N2} " +
                         $"via {notification.PaymentMethod}. Reason: {notification.FailureReason}. " +
                         $"Error: {notification.ErrorCode}";

            await _notificationService.CreateNotificationAsync(
                notification.UserId,
                "Payment Failed",
                message,
                "Payment",
                cancellationToken);

            _logger.LogInformation(
                "In-app notification created for failed payment Transaction {TransactionId}",
                notification.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create in-app notification for Transaction {TransactionId}",
                notification.TransactionId);
        }
    }

    private async Task SendEmailNotificationAsync(
        PaymentFailedEvent notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var subject = "Payment Failed - Action Required";
            var body = $@"
Dear Customer,

Unfortunately, your payment could not be processed.

Payment Details:
?????????????????????????????????????????
Amount:              {notification.Currency} {notification.Amount / 100.0:N2}
Payment Method:      {notification.PaymentMethod}
Failure Reason:      {notification.FailureReason}
Error Code:          {notification.ErrorCode}
Attempted Date:      {notification.FailedAt:yyyy-MM-dd HH:mm:ss} UTC

Action Required:
- Please verify your card details and try again
- Ensure your card has sufficient funds
- Check that your card hasn't expired
- Contact your bank if the issue persists

If you need further assistance, please contact our customer support.

Best regards,
Banking System Team

---
This is an automated message. Please do not reply to this email.
For assistance, please contact our customer support.
";

            await _emailService.SendEmailAsync(
                notification.UserEmail,
                subject,
                body,
                cancellationToken);

            _logger.LogInformation(
                "Payment failure notification sent to {Email} for Transaction {TransactionId}",
                notification.UserEmail,
                notification.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send payment failure notification to {Email} for Transaction {TransactionId}",
                notification.UserEmail,
                notification.TransactionId);
        }
    }
}
