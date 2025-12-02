// src\BankingSystem.Application\Commands\Accounts\Handlers\CreateAccountHandler.cs
using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Commands.Accounts;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Enums;
using BankingSystem.Domain.ValueObjects;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Constants;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Application.Commands.Accounts.Handlers;

public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateAccountHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
            throw new NotFoundException(string.Format(ValidationMessages.UserNotFound, request.UserId));

        var accountNumber = GenerateAccountNumber();

        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            AccountNumber = accountNumber,
            AccountType = Enum.Parse<AccountType>(request.AccountType),
            Balance = new Money(0, "USD"),
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IBAN = request.IBAN,
            BIC = request.BIC
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);

        return account.Id;
    }

    private static string GenerateAccountNumber()
    {
        return DateTime.UtcNow.Ticks.ToString().Substring(0, 10) + Random.Shared.Next(1000, 9999);
    }
}
