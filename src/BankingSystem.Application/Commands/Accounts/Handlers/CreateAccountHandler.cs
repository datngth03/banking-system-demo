// src\BankingSystem.Application\Commands\Accounts\Handlers\CreateAccountHandler.cs
using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Commands.Accounts;
using BankingSystem.Application.Models;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Enums;
using BankingSystem.Domain.ValueObjects;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BankingSystem.Application.Commands.Accounts.Handlers;

public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CreateAccountHandler> _logger;

    public CreateAccountHandler(
        IApplicationDbContext context,
        ILogger<CreateAccountHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        
        if (user == null)
            throw new NotFoundException(string.Format(ValidationMessages.UserNotFound, request.UserId));

        var accountNumber = GenerateAccountNumber();
        var accountType = Enum.Parse<AccountType>(request.AccountType);

        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            AccountNumber = accountNumber,
            AccountType = accountType,
            Balance = new Money(0, "USD"),
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IBAN = request.IBAN,
            BIC = request.BIC
        };

        _context.Accounts.Add(account);

        // Create Outbox Message for async notification/email
        var accountCreatedData = new
        {
            UserId = user.Id,
            UserEmail = user.Email,
            UserFirstName = user.FirstName,
            UserLastName = user.LastName,
            AccountId = account.Id,
            AccountNumber = account.AccountNumber,
            AccountType = accountType.ToString(),
            InitialBalance = account.Balance.Amount,
            Currency = account.Balance.Currency,
            IBAN = account.IBAN,
            BIC = account.BIC,
            CreatedAt = account.CreatedAt
        };

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "AccountCreatedEvent",
            EventData = JsonSerializer.Serialize(accountCreatedData),
            CreatedAt = DateTime.UtcNow,
            IsProcessed = false
        };

        _context.OutboxMessages.Add(outboxMessage);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Account {AccountId} created for user {UserId}. Outbox message {OutboxId} created.",
            account.Id,
            user.Id,
            outboxMessage.Id);

        return account.Id;
    }

    private static string GenerateAccountNumber()
    {
        return DateTime.UtcNow.Ticks.ToString().Substring(0, 10) + Random.Shared.Next(1000, 9999);
    }
}
