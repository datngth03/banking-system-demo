namespace BankingSystem.Application.Interfaces;

/// <summary>
/// Service for sending emails
/// All methods support CancellationToken for proper async/await patterns
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a simple email
    /// </summary>
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send an email with CC recipients
    /// </summary>
    Task SendEmailAsync(string to, string[] cc, string subject, string body, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send password reset email
    /// </summary>
    Task SendPasswordResetEmailAsync(string to, string resetToken, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send welcome email to new user
    /// </summary>
    Task SendWelcomeEmailAsync(string to, string firstName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send transaction confirmation email
    /// </summary>
    Task SendTransactionConfirmationEmailAsync(string to, string accountNumber, decimal amount, string transactionType, CancellationToken cancellationToken = default);
}
