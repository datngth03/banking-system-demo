namespace BankingSystem.Infrastructure.Services;

using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _emailSettings;

    public EmailService(
        ILogger<EmailService> logger,
        IOptions<EmailSettings> emailSettings)
    {
        _logger = logger;
        _emailSettings = emailSettings.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = CreateSmtpClient();
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Email sent successfully to {Email}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", to);
            throw;
        }
    }

    public async Task SendEmailAsync(string to, string[] cc, string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = CreateSmtpClient();
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(to);

            foreach (var ccEmail in cc)
            {
                mailMessage.CC.Add(ccEmail);
            }

            await client.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Email sent successfully to {Email} with CC {Cc}", to, string.Join(",", cc));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", to);
            throw;
        }
    }

    public async Task SendPasswordResetEmailAsync(string to, string resetToken, CancellationToken cancellationToken = default)
    {
        var resetLink = $"https://yourdomain.com/reset-password?token={resetToken}";
        var body = $@"
            <h2>Password Reset</h2>
            <p>Click the link below to reset your password:</p>
            <a href='{resetLink}'>Reset Password</a>
            <p>This link will expire in 24 hours.</p>";

        await SendEmailAsync(to, "Password Reset Request", body, cancellationToken);
    }

    public async Task SendWelcomeEmailAsync(string to, string firstName, CancellationToken cancellationToken = default)
    {
        var body = $@"
            <h2>Welcome to Banking System!</h2>
            <p>Hello {firstName},</p>
            <p>Thank you for creating an account with us. We're excited to have you on board!</p>
            <p>Visit our website to get started.</p>";

        await SendEmailAsync(to, "Welcome to Banking System", body, cancellationToken);
    }

    public async Task SendTransactionConfirmationEmailAsync(string to, string accountNumber, decimal amount, string transactionType, CancellationToken cancellationToken = default)
    {
        var body = $@"
            <h2>Transaction Confirmation</h2>
            <p>Your {transactionType} transaction has been completed successfully.</p>
            <ul>
                <li><strong>Account:</strong> {accountNumber}</li>
                <li><strong>Amount:</strong> {amount:C}</li>
                <li><strong>Type:</strong> {transactionType}</li>
                <li><strong>Date:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</li>
            </ul>";

        await SendEmailAsync(to, "Transaction Confirmation", body, cancellationToken);
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
        {
            EnableSsl = _emailSettings.EnableSsl,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrEmpty(_emailSettings.Username) && !string.IsNullOrEmpty(_emailSettings.Password))
        {
            client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
        }

        return client;
    }
}
