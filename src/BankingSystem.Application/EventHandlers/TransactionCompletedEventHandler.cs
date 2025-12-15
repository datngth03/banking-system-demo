namespace BankingSystem.Application.EventHandlers;

using BankingSystem.Application.Events;
using BankingSystem.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles TransactionCompletedEvent to send notifications and emails
/// when a transaction is successfully completed via the Outbox pattern.
/// </summary>
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
        _logger.LogInformation(
            "Processing transaction completion for Transaction {TransactionId}, User {UserId}",
            notification.TransactionId,
            notification.UserId);

        try
        {
            // 1. Create in-app notification
            await CreateNotificationAsync(notification, cancellationToken);

            // 2. Send email notification for significant transactions
            await SendEmailNotificationAsync(notification, cancellationToken);

            _logger.LogInformation(
                "Successfully processed transaction completion for Transaction {TransactionId}",
                notification.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing transaction completion for Transaction {TransactionId}",
                notification.TransactionId);
            // Don't rethrow - we don't want to mark the outbox message as failed
        }
    }

    private async Task CreateNotificationAsync(
        TransactionCompletedEvent notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var transactionSign = GetTransactionSign(notification.TransactionType);
            var message = $"{notification.TransactionType}: {transactionSign}{notification.Currency} {notification.Amount:N2}. " +
                         $"New balance: {notification.Currency} {notification.BalanceAfter:N2}. " +
                         $"Ref: {notification.TransactionReference}";

            await _notificationService.CreateNotificationAsync(
                notification.UserId,
                "Transaction Completed",
                message,
                "Transaction",
                cancellationToken);

            _logger.LogInformation(
                "In-app notification created for User {UserId}, Transaction {TransactionId}",
                notification.UserId,
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
        TransactionCompletedEvent notification,
        CancellationToken cancellationToken)
    {
        // Only send email for significant transactions (> 100 or withdrawal/deposit)
        if (!ShouldSendEmail(notification))
        {
            _logger.LogDebug(
                "Skipping email notification for Transaction {TransactionId} (not significant)",
                notification.TransactionId);
            return;
        }

        if (string.IsNullOrEmpty(notification.UserEmail))
        {
            _logger.LogWarning(
                "No email address found for User {UserId}, skipping email notification",
                notification.UserId);
            return;
        }

        try
        {
            var subject = $"Transaction Confirmation - {notification.TransactionType}";
            var body = BuildEmailBody(notification);

            await _emailService.SendEmailAsync(
                notification.UserEmail,
                subject,
                body,
                cancellationToken);

            _logger.LogInformation(
                "Email notification sent to {Email} for Transaction {TransactionId}",
                notification.UserEmail,
                notification.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email notification for Transaction {TransactionId} to {Email}",
                notification.TransactionId,
                notification.UserEmail);
        }
    }

    private bool ShouldSendEmail(TransactionCompletedEvent notification)
    {
        // Send email for deposits, withdrawals, and large transactions
        var significantTypes = new[] { "Deposit", "Withdrawal", "Transfer" };
        return significantTypes.Contains(notification.TransactionType) || notification.Amount >= 100;
    }

    private string GetTransactionSign(string transactionType)
    {
        return transactionType switch
        {
            "Deposit" or "InterestCredit" or "Refund" => "+",
            "Withdrawal" or "Fee" or "BillPayment" or "Transfer" => "-",
            _ => ""
        };
    }

    private string BuildEmailBody(TransactionCompletedEvent notification)
    {
        var userName = !string.IsNullOrEmpty(notification.UserFirstName)
            ? $"{notification.UserFirstName} {notification.UserLastName}"
            : "Valued Customer";

        var transactionSign = GetTransactionSign(notification.TransactionType);

        return $@"
Dear {userName},

Your transaction has been successfully processed.

Transaction Details:
?????????????????????????????????????????
Transaction Type:    {notification.TransactionType}
Amount:              {transactionSign}{notification.Currency} {notification.Amount:N2}
Reference Number:    {notification.TransactionReference}
Transaction Date:    {notification.TransactionDate:yyyy-MM-dd HH:mm:ss} UTC
Description:         {notification.Description}

Account Information:
?????????????????????????????????????????
Account Number:      {notification.AccountNumber}
Balance Before:      {notification.Currency} {notification.BalanceBefore:N2}
Balance After:       {notification.Currency} {notification.BalanceAfter:N2}
Change:              {transactionSign}{notification.Currency} {notification.Amount:N2}

If you did not authorize this transaction, please contact our customer support immediately.

Best regards,
Banking System Team

---
This is an automated message. Please do not reply to this email.
For assistance, please contact our customer support.
";
    }
}
