namespace BankingSystem.Application.Queries.Transactions;

using MediatR;
using BankingSystem.Application.DTOs.Transactions;

public class GetTransactionsByUserIdQuery : IRequest<IEnumerable<TransactionDto>>
{
    public Guid UserId { get; set; }
}
