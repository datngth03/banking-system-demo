namespace BankingSystem.Application.Queries.Accounts.Handlers;

using BankingSystem.Application.DTOs.Accounts;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Queries.Accounts;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetAccountsByUserIdHandler : IRequestHandler<GetAccountsByUserIdQuery, IEnumerable<AccountDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAccountsByUserIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AccountDto>> Handle(GetAccountsByUserIdQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _context.Accounts
            .Where(a => a.UserId == request.UserId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AccountDto
            {
                Id = a.Id,
                UserId = a.UserId,
                AccountNumber = a.AccountNumber,
                AccountType = a.AccountType.ToString(),
                Balance = a.Balance.Amount,
                Currency = a.Balance.Currency,
                CreatedAt = a.CreatedAt,
                ClosedAt = a.ClosedAt,
                IsActive = a.IsActive,
                IBAN = a.IBAN,
                BIC = a.BIC
            })
            .ToListAsync(cancellationToken);

        return accounts;
    }
}
