namespace BankingSystem.Application.Commands.Accounts;

using MediatR;

public class CreateAccountCommand : IRequest<Guid>
{
    public Guid UserId { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public string? IBAN { get; set; }
    public string? BIC { get; set; }
}
