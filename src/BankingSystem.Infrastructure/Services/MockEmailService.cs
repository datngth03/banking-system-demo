namespace BankingSystem.Infrastructure.Services;

using BankingSystem.Application.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// Mock Email Service for Development/Testing
/// Logs email content instead of actually sending emails
/// </summary>
public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            """
            ========================================
            ?? MOCK EMAIL (Not Actually Sent)
            ========================================
            To: {To}
            Subject: {Subject}
            ----------------------------------------
            {Body}
            ========================================
            """,
            to, subject, body);

        return Task.CompletedTask;
    }

    public Task SendEmailAsync(string to, string[] cc, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            """
            ========================================
            ?? MOCK EMAIL (Not Actually Sent)
            ========================================
            To: {To}
            CC: {CC}
            Subject: {Subject}
            ----------------------------------------
            {Body}
            ========================================
            """,
            to, string.Join(", ", cc), subject, body);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string to, string resetToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            """
            ========================================
            ?? MOCK PASSWORD RESET EMAIL
            ========================================
            To: {To}
            Reset Token: {Token}
            ========================================
            """,
            to, resetToken);

        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string to, string firstName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            """
            ========================================
            ?? MOCK WELCOME EMAIL
            ========================================
            To: {To}
            FirstName: {FirstName}
            ========================================
            """,
            to, firstName);

        return Task.CompletedTask;
    }

    public Task SendTransactionConfirmationEmailAsync(string to, string accountNumber, decimal amount, string transactionType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            """
            ========================================
            ?? MOCK TRANSACTION CONFIRMATION
            ========================================
            To: {To}
            Account: {Account}
            Type: {Type}
            Amount: {Amount:C}
            ========================================
            """,
            to, accountNumber, transactionType, amount);

        return Task.CompletedTask;
    }
}
