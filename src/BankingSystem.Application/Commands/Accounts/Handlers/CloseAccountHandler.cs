using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Commands.Accounts;
using Microsoft.EntityFrameworkCore;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Constants;

namespace BankingSystem.Application.Commands.Accounts.Handlers;

public class CloseAccountHandler : IRequestHandler<CloseAccountCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public CloseAccountHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(CloseAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);
        if (account == null)
            throw new NotFoundException(string.Format(ValidationMessages.AccountNotFound, request.AccountId));

        if (account.Balance.Amount > 0)
            throw new BankingApplicationException(ValidationMessages.CannotCloseAccountWithBalance);

        account.IsActive = false;
        account.ClosedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
