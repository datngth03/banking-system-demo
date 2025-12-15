namespace BankingSystem.Application.EventHandlers;

using BankingSystem.Application.Events;
using BankingSystem.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles AccountCreatedEvent to send notifications and emails
/// when an account is successfully created via the Outbox pattern.
/// </summary>
public class AccountCreatedEventHandler : INotificationHandler<AccountCreatedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountCreatedEventHandler> _logger;

    public AccountCreatedEventHandler(
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<AccountCreatedEventHandler> logger)
    {
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(AccountCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing account creation for Account {AccountId}, User {UserId}",
            notification.AccountId,
            notification.UserId);

        try
        {
            // 1. Create in-app notification
            await CreateNotificationAsync(notification, cancellationToken);

            // 2. Send welcome email
            await SendWelcomeEmailAsync(notification, cancellationToken);

            _logger.LogInformation(
                "Successfully processed account creation for Account {AccountId}",
                notification.AccountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing account creation for Account {AccountId}",
                notification.AccountId);
            // Don't rethrow - we don't want to mark the outbox message as failed
        }
    }

    private async Task CreateNotificationAsync(
        AccountCreatedEvent notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = $"Your {notification.AccountType} account ({notification.AccountNumber}) " +
                         $"has been successfully created. Welcome to our banking services!";

            await _notificationService.CreateNotificationAsync(
                notification.UserId,
                "New Account Created",
                message,
                "AccountCreated",
                cancellationToken);

            _logger.LogInformation(
                "In-app notification created for User {UserId}, Account {AccountId}",
                notification.UserId,
                notification.AccountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create in-app notification for Account {AccountId}",
                notification.AccountId);
        }
    }

    private async Task SendWelcomeEmailAsync(
        AccountCreatedEvent notification,
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
            var subject = "Welcome to Banking System - Account Created";
            var body = BuildWelcomeEmailBody(notification);

            await _emailService.SendEmailAsync(
                notification.UserEmail,
                subject,
                body,
                cancellationToken);

            _logger.LogInformation(
                "Welcome email sent to {Email} for Account {AccountId}",
                notification.UserEmail,
                notification.AccountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send welcome email for Account {AccountId} to {Email}",
                notification.AccountId,
                notification.UserEmail);
        }
    }

    private string BuildWelcomeEmailBody(AccountCreatedEvent notification)
    {
        var userName = !string.IsNullOrEmpty(notification.UserFirstName)
            ? $"{notification.UserFirstName} {notification.UserLastName}"
            : "Valued Customer";

        var ibanInfo = !string.IsNullOrEmpty(notification.IBAN)
            ? $@"
IBAN:                {notification.IBAN}"
            : string.Empty;

        var bicInfo = !string.IsNullOrEmpty(notification.BIC)
            ? $@"
BIC/SWIFT:           {notification.BIC}"
            : string.Empty;

        return $@"
Dear {userName},

Welcome to Banking System! ??

We're excited to inform you that your new account has been successfully created.

Account Details:
?????????????????????????????????????????
Account Number:      {notification.AccountNumber}
Account Type:        {notification.AccountType}
Currency:            {notification.Currency}
Initial Balance:     {notification.Currency} {notification.InitialBalance:N2}{ibanInfo}{bicInfo}
Created On:          {notification.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC
?????????????????????????????????????????

What's Next?
• Start making deposits and transfers
• Set up recurring payments
• Apply for a debit/credit card
• Explore our mobile banking app

Need Help?
Our customer support team is available 24/7 to assist you with any questions.

Thank you for choosing Banking System!

Best regards,
The Banking System Team

---
This is an automated message. Please do not reply to this email.
For assistance, please contact our customer support.
";
    }
}
