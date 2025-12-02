namespace BankingSystem.Application.Commands.Accounts;

using MediatR;

public class CloseAccountCommand : IRequest<Unit>
{
    public Guid AccountId { get; set; }
}
