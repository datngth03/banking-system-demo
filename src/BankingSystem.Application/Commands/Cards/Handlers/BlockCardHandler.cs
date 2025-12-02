//src\BankingSystem.Application\Commands\Cards\Handlers\BlockCardHandler.cs
using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Commands.Cards;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Constants;
using BankingSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankingSystem.Application.Commands.Cards.Handlers;

public class BlockCardHandler : IRequestHandler<BlockCardCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<BlockCardHandler> _logger;

    public BlockCardHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICacheService cacheService,
        ILogger<BlockCardHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Unit> Handle(BlockCardCommand request, CancellationToken cancellationToken)
    {
        var card = await _context.Cards
            .FirstOrDefaultAsync(c => c.Id == request.CardId, cancellationToken);

        if (card == null)
            throw new NotFoundException(string.Format(ValidationMessages.CardNotFound, request.CardId));

        // Authorization: Users can block their own cards, staff can block any card
        if (!_currentUserService.IsStaff && card.UserId != _currentUserService.UserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to block card {CardId} owned by {OwnerId}",
                _currentUserService.UserId,
                card.Id,
                card.UserId);

            throw new ForbiddenException("You can only block your own cards");
        }

        if (card.Status == CardStatus.Blocked)
        {
            throw new BankingApplicationException(ValidationMessages.CardBlocked);
        }

        card.Status = CardStatus.Blocked;
        card.BlockedAt = DateTime.UtcNow;
        card.BlockedReason = request.Reason;

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate card caches
        await _cacheService.RemoveAsync($"card:{card.Id}", cancellationToken);
        await _cacheService.RemoveAsync($"card:user:{card.UserId}:page:1", cancellationToken); // Invalidate first page
        await _cacheService.RemoveAsync($"card:account:{card.AccountId}", cancellationToken);

        _logger.LogInformation(
            "Card {CardId} blocked by user {UserId}. Reason: {Reason}",
            card.Id,
            _currentUserService.UserId,
            request.Reason);

        return Unit.Value;
    }
}
