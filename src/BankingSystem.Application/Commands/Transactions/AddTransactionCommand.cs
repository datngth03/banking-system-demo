namespace BankingSystem.Application.Commands.Transactions;

using MediatR;

public class AddTransactionCommand : IRequest<Guid>
{
    public Guid AccountId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}
