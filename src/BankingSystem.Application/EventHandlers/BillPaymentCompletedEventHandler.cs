namespace BankingSystem.Application.EventHandlers;

using BankingSystem.Application.Events;
using BankingSystem.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles BillPaymentCompletedEvent to send notifications and emails
/// when a bill payment is successfully processed via the Outbox pattern.
/// </summary>
public class BillPaymentCompletedEventHandler : INotificationHandler<BillPaymentCompletedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<BillPaymentCompletedEventHandler> _logger;

    public BillPaymentCompletedEventHandler(
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<BillPaymentCompletedEventHandler> logger)
    {
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(BillPaymentCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing bill payment completion for Bill {BillId}, Transaction {TransactionId}, User {UserId}",
            notification.BillId,
            notification.TransactionId,
            notification.UserId);

        try
        {
            // 1. Create in-app notification
            await CreateNotificationAsync(notification, cancellationToken);

            // 2. Send email notification
            await SendEmailNotificationAsync(notification, cancellationToken);

            _logger.LogInformation(
                "Successfully processed bill payment completion for Bill {BillId}",
                notification.BillId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing bill payment completion for Bill {BillId}",
                notification.BillId);
            // Don't rethrow - we don't want to mark the outbox message as failed
            // The notification/email failure shouldn't affect the bill payment itself
        }
    }

    private async Task CreateNotificationAsync(
        BillPaymentCompletedEvent notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = $"Bill payment to {notification.Biller} ({notification.Currency} {notification.Amount:N2}) " +
                         $"has been successfully processed. " +
                         $"Reference: {notification.TransactionReference}";

            await _notificationService.CreateNotificationAsync(
                notification.UserId,
                "Bill Payment Successful",
                message,
                "BillPayment",
                cancellationToken);

            _logger.LogInformation(
                "In-app notification created for User {UserId}, Bill {BillId}",
                notification.UserId,
                notification.BillId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create in-app notification for Bill {BillId}",
                notification.BillId);
        }
    }

    private async Task SendEmailNotificationAsync(
        BillPaymentCompletedEvent notification,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(notification.UserEmail))
        {
            _logger.LogWarning(
                "No email address found for User {UserId}, skipping email notification",
                notification.UserId);
            return;
        }

        try
        {
            var subject = "Bill Payment Confirmation";
            var body = BuildEmailBody(notification);

            await _emailService.SendEmailAsync(
                notification.UserEmail,
                subject,
                body,
                cancellationToken);

            _logger.LogInformation(
                "Email notification sent to {Email} for Bill {BillId}",
                notification.UserEmail,
                notification.BillId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email notification for Bill {BillId} to {Email}",
                notification.BillId,
                notification.UserEmail);
        }
    }

    private string BuildEmailBody(BillPaymentCompletedEvent notification)
    {
        var userName = !string.IsNullOrEmpty(notification.UserFirstName)
            ? $"{notification.UserFirstName} {notification.UserLastName}"
            : "Valued Customer";

        return $@"
Dear {userName},

Your bill payment has been successfully processed.

Payment Details:
?????????????????????????????????????????
Biller:              {notification.Biller}
Bill Number:         {notification.BillNumber}
Amount Paid:         {notification.Currency} {notification.Amount:N2}
Payment Date:        {notification.PaidDate:yyyy-MM-dd HH:mm:ss} UTC
Reference Number:    {notification.TransactionReference}

Account Information:
?????????????????????????????????????????
Account Number:      {notification.AccountNumber}
New Balance:         {notification.Currency} {notification.NewBalance:N2}

Thank you for using our banking services!

Best regards,
Banking System Team

---
This is an automated message. Please do not reply to this email.
If you have any questions, please contact our customer support.
";
    }
}
