namespace BankingSystem.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendEmailAsync(string to, string[] cc, string subject, string body);
    Task SendPasswordResetEmailAsync(string to, string resetToken);
    Task SendWelcomeEmailAsync(string to, string firstName);
    Task SendTransactionConfirmationEmailAsync(string to, string accountNumber, decimal amount, string transactionType);
}
