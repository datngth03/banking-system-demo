using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Commands.Cards;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Enums;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankingSystem.Application.Commands.Cards.Handlers;

public class IssueCardHandler : IRequestHandler<IssueCardCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDataEncryptionService _encryptionService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<IssueCardHandler> _logger;
    private readonly IAuditLogService _auditLogService;

    public IssueCardHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDataEncryptionService encryptionService,
        ICacheService cacheService,
        ILogger<IssueCardHandler> logger,
        IAuditLogService auditLogService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _encryptionService = encryptionService;
        _cacheService = cacheService;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    public async Task<Guid> Handle(IssueCardCommand request, CancellationToken cancellationToken)
    {
        // Verify account exists
        var account = await _context.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

        if (account == null)
            throw new NotFoundException(string.Format(ValidationMessages.AccountNotFound, request.AccountId));

        // Authorization: Users can only issue cards for their own accounts, staff can issue for any
        if (!_currentUserService.IsStaff && account.UserId != _currentUserService.UserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to issue card for account {AccountId} owned by {OwnerId}",
                _currentUserService.UserId,
                account.Id,
                account.UserId);

            throw new ForbiddenException("You can only issue cards for your own accounts");
        }

        // Generate card number and CVV
        var cardNumber = GenerateCardNumber();
        var cvv = GenerateCVV();
        var expiryDate = DateTime.UtcNow.AddYears(3); // Cards valid for 3 years

        var card = new Card
        {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId,
            UserId = account.UserId,
            CardNumber = cardNumber,
            CVV = cvv,
            ExpiryDate = expiryDate,
            CardHolderName = request.NameOnCard ?? $"{account.User!.FirstName} {account.User.LastName}",
            Status = CardStatus.Active, // Cards start as Active
            CreatedAt = DateTime.UtcNow
        };

        // Encrypt sensitive data before saving
        card.EncryptSensitiveData(_encryptionService.Encrypt);

        _context.Cards.Add(card);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate user's card cache
        await _cacheService.RemoveAsync($"card:user:{account.UserId}:page:1", cancellationToken);
        await _cacheService.RemoveAsync($"card:account:{account.Id}", cancellationToken);

        _logger.LogInformation(
            "Issued new card {CardId} for account {AccountId}, user {UserId}. Masked card: {MaskedCard}",
            card.Id,
            account.Id,
            account.UserId,
            card.MaskedCardNumber);

        // Audit log
        await _auditLogService.LogAuditAsync(
            "Card",
            "Issue",
            card.Id,
            account.UserId,
            null,
            new { CardId = card.Id, AccountId = account.Id, CardHolderName = card.CardHolderName },
            cancellationToken);

        return card.Id;
    }

    private static string GenerateCardNumber()
    {
        // Simple card number generation (first 6 digits = BIN, last digit = checksum)
        // In production, use proper Luhn algorithm
        var random = new Random();
        var bin = "456789"; // Bank Identification Number
        var accountNumber = random.Next(100000000, 999999999).ToString();
        return $"{bin}{accountNumber}".Substring(0, 16);
    }

    private static string GenerateCVV()
    {
        var random = new Random();
        return random.Next(100, 999).ToString();
    }
}
