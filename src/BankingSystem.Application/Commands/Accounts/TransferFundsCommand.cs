namespace BankingSystem.Application.Commands.Accounts;

using MediatR;

public class TransferFundsCommand : IRequest<Unit>
{
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}
