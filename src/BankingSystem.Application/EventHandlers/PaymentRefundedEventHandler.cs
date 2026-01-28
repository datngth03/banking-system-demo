namespace BankingSystem.Application.EventHandlers;

using BankingSystem.Application.Events;
using BankingSystem.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles PaymentRefundedEvent to send refund confirmation notifications
/// </summary>
public class PaymentRefundedEventHandler : INotificationHandler<PaymentRefundedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<PaymentRefundedEventHandler> _logger;

    public PaymentRefundedEventHandler(
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<PaymentRefundedEventHandler> logger)
    {
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(PaymentRefundedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing refund notification for Transaction {TransactionId}, User {UserId}, Refund Amount {RefundAmount}",
            notification.TransactionId,
            notification.UserId,
            notification.RefundAmount);

        try
        {
            // Create in-app notification
            await CreateNotificationAsync(notification, cancellationToken);

            // Send refund confirmation email
            if (!string.IsNullOrEmpty(notification.UserEmail))
            {
                await SendEmailNotificationAsync(notification, cancellationToken);
            }

            _logger.LogInformation(
                "Successfully processed refund notification for Transaction {TransactionId}",
                notification.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing refund notification for Transaction {TransactionId}",
                notification.TransactionId);
        }
    }

    private async Task CreateNotificationAsync(
        PaymentRefundedEvent notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = $"Refund Processed: {notification.Currency} {notification.RefundAmount / 100.0:N2} " +
                         $"refunded from {notification.StripePaymentId}. " +
                         $"Reason: {notification.RefundReason}. Ref: {notification.RefundId}";

            await _notificationService.CreateNotificationAsync(
                notification.UserId,
                "Refund Processed",
                message,
                "Refund",
                cancellationToken);

            _logger.LogInformation(
                "In-app notification created for refund Transaction {TransactionId}",
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
        PaymentRefundedEvent notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var userName = !string.IsNullOrEmpty(notification.UserFirstName)
                ? $"{notification.UserFirstName} {notification.UserLastName}"
                : "Valued Customer";

            var subject = "Refund Confirmation - Your Refund Has Been Processed";
            var originalAmount = notification.Amount / 100.0;
            var refundAmount = notification.RefundAmount / 100.0;
            var body = $@"
Dear {userName},

Your refund has been successfully processed!

Refund Details:
?????????????????????????????????????????
Original Transaction:      {notification.StripePaymentId}
Original Amount:           {notification.Currency} {originalAmount:N2}
Refund Amount:             {notification.Currency} {refundAmount:N2}
Refund ID:                 {notification.RefundId}
Refund Reason:             {notification.RefundReason}
Processed Date:            {notification.RefundedAt:yyyy-MM-dd HH:mm:ss} UTC

Timeline:
- Your refund has been initiated
- The amount will be credited back to your original payment method
- Please allow 3-5 business days for the refund to appear in your account
- You'll receive another notification once the refund is fully processed

If you have questions about this refund, please contact our customer support.

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
                "Refund confirmation email sent to {Email} for Transaction {TransactionId}",
                notification.UserEmail,
                notification.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send refund confirmation email to {Email} for Transaction {TransactionId}",
                notification.UserEmail,
                notification.TransactionId);
        }
    }
}
