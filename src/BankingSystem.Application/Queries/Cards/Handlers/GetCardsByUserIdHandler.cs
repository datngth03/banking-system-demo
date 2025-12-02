namespace BankingSystem.Application.Queries.Cards.Handlers;

using BankingSystem.Application.DTOs.Cards;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Queries.Cards;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetCardsByUserIdHandler : IRequestHandler<GetCardsByUserIdQuery, IEnumerable<CardDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDataEncryptionService _encryptionService;
    private readonly ICacheService _cacheService;

    public GetCardsByUserIdHandler(
        IApplicationDbContext context,
        IDataEncryptionService encryptionService,
        ICacheService cacheService)
    {
        _context = context;
        _encryptionService = encryptionService;
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<CardDto>> Handle(GetCardsByUserIdQuery request, CancellationToken cancellationToken)
    {
        // 1. Try cache first (3-minute TTL)
        var cacheKey = $"card:user:{request.UserId}:page:{request.PageNumber}";
        var cachedCards = await _cacheService.GetAsync<List<CardDto>>(cacheKey, cancellationToken);

        if (cachedCards != null)
            return cachedCards;

        // 2. Query database with optimizations
        var cards = await _context.Cards
            .AsNoTracking()
            .Where(c => c.UserId == request.UserId)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // 3. Decrypt sensitive data and map to DTOs
        var cardDtos = cards.Select(c =>
        {
            // Decrypt sensitive data
            c.DecryptSensitiveData(_encryptionService.Decrypt);

            return new CardDto
            {
                Id = c.Id,
                UserId = c.UserId,
                AccountId = c.AccountId,
                // Use masked card number for security
                CardNumber = c.MaskedCardNumber,
                CardHolderName = c.CardHolderName,
                CVV = c.MaskedCVV, // Always masked
                ExpiryDate = c.ExpiryDate,
                Status = c.Status.ToString(),
                CreatedAt = c.CreatedAt,
                BlockedAt = c.BlockedAt,
                BlockedReason = c.BlockedReason
            };
        }).ToList();

        // 4. Cache for 3 minutes
        await _cacheService.SetAsync(cacheKey, cardDtos, TimeSpan.FromMinutes(3), cancellationToken);

        return cardDtos;
    }
}
