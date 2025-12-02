namespace BankingSystem.Application.Queries.Accounts;

using MediatR;
using BankingSystem.Application.DTOs.Accounts;

public class GetAccountDetailsQuery : IRequest<AccountDto?>
{
    public Guid AccountId { get; set; }
}
