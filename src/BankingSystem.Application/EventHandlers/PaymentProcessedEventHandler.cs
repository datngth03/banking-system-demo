namespace BankingSystem.Application.EventHandlers;

using BankingSystem.Application.Events;
using BankingSystem.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles PaymentProcessedEvent to send success notifications via in-app notifications and emails
/// </summary>
public class PaymentProcessedEventHandler : INotificationHandler<PaymentProcessedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<PaymentProcessedEventHandler> _logger;

    public PaymentProcessedEventHandler(
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<PaymentProcessedEventHandler> logger)
    {
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(PaymentProcessedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing payment success notification for Transaction {TransactionId}, User {UserId}, Amount {Amount}",
            notification.TransactionId,
            notification.UserId,
            notification.Amount);

        try
        {
            // Create in-app notification
            await CreateNotificationAsync(notification, cancellationToken);

            // Send email confirmation
            if (!string.IsNullOrEmpty(notification.UserEmail))
            {
                await SendEmailNotificationAsync(notification, cancellationToken);
            }

            _logger.LogInformation(
                "Successfully processed payment success notification for Transaction {TransactionId}",
                notification.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing payment success notification for Transaction {TransactionId}",
                notification.TransactionId);
        }
    }

    private async Task CreateNotificationAsync(
        PaymentProcessedEvent notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = $"Payment Successful: {notification.Currency} {notification.Amount / 100.0:N2} " +
                         $"via {notification.PaymentMethod}. Ref: {notification.StripePaymentId}";

            await _notificationService.CreateNotificationAsync(
                notification.UserId,
                "Payment Successful",
                message,
                "Payment",
                cancellationToken);

            _logger.LogInformation(
                "In-app notification created for payment Transaction {TransactionId}",
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
        PaymentProcessedEvent notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var userName = !string.IsNullOrEmpty(notification.UserFirstName)
                ? $"{notification.UserFirstName} {notification.UserLastName}"
                : "Valued Customer";

            var subject = "Payment Confirmation - Your Transaction is Complete";
            var body = $@"
Dear {userName},

Your payment has been successfully processed!

Payment Details:
?????????????????????????????????????????
Amount:              {notification.Currency} {notification.Amount / 100.0:N2}
Payment Method:      {notification.PaymentMethod}
Stripe Confirmation: {notification.StripePaymentId}
Status:              {notification.PaymentStatus}
Processed Date:      {notification.ProcessedAt:yyyy-MM-dd HH:mm:ss} UTC

{(string.IsNullOrEmpty(notification.ReceiptUrl) ? "" : $"Receipt: {notification.ReceiptUrl}")}

If you have any questions about this payment, please contact our customer support.

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
                "Email confirmation sent to {Email} for Transaction {TransactionId}",
                notification.UserEmail,
                notification.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send payment confirmation email to {Email} for Transaction {TransactionId}",
                notification.UserEmail,
                notification.TransactionId);
        }
    }
}
