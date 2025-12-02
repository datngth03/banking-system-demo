namespace BankingSystem.Application.Queries.Cards.Handlers;

using BankingSystem.Application.DTOs.Cards;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Queries.Cards;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetCardsByAccountIdHandler : IRequestHandler<GetCardsByAccountIdQuery, IEnumerable<CardDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCardsByAccountIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CardDto>> Handle(GetCardsByAccountIdQuery request, CancellationToken cancellationToken)
    {
        var cards = await _context.Cards
            .Where(c => c.AccountId == request.AccountId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CardDto
            {
                Id = c.Id,
                UserId = c.UserId,
                AccountId = c.AccountId,
                // Mask card number for security - show only last 4 digits
                CardNumber = "****-****-****-" + c.CardNumber.Substring(c.CardNumber.Length - 4),
                CardHolderName = c.CardHolderName,
                CVV = "***", // Never expose CVV
                ExpiryDate = c.ExpiryDate,
                Status = c.Status.ToString(),
                CreatedAt = c.CreatedAt,
                BlockedAt = c.BlockedAt,
                BlockedReason = c.BlockedReason
            })
            .ToListAsync(cancellationToken);

        return cards;
    }
}
