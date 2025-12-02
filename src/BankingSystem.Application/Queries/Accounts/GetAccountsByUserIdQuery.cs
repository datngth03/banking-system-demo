namespace BankingSystem.Application.Queries.Accounts;

using BankingSystem.Application.DTOs.Accounts;
using MediatR;

public class GetAccountsByUserIdQuery : IRequest<IEnumerable<AccountDto>>
{
    public Guid UserId { get; set; }
}
