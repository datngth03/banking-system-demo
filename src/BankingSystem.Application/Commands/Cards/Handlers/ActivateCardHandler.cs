using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Commands.Cards;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Constants;
using BankingSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankingSystem.Application.Commands.Cards.Handlers;

public class ActivateCardHandler : IRequestHandler<ActivateCardCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ActivateCardHandler> _logger;

    public ActivateCardHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<ActivateCardHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Unit> Handle(ActivateCardCommand request, CancellationToken cancellationToken)
    {
        var card = await _context.Cards
            .FirstOrDefaultAsync(c => c.Id == request.CardId, cancellationToken);

        if (card == null)
            throw new NotFoundException(string.Format(ValidationMessages.CardNotFound, request.CardId));

        // Authorization: Users can only activate their own cards
        if (!_currentUserService.IsStaff && card.UserId != _currentUserService.UserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to activate card {CardId} owned by {OwnerId}",
                _currentUserService.UserId,
                card.Id,
                card.UserId);

            throw new ForbiddenException("You can only activate your own cards");
        }

        // Verify last 4 digits for security
        var last4 = card.CardNumber.Substring(card.CardNumber.Length - 4);
        if (last4 != request.LastFourDigits)
        {
            _logger.LogWarning(
                "Invalid last 4 digits provided for card activation {CardId}",
                card.Id);

            throw new ValidationFailureException("Invalid card verification. Please check the last 4 digits.");
        }

        if (card.Status == CardStatus.Active)
        {
            throw new BankingApplicationException("Card is already activated");
        }

        if (card.Status == CardStatus.Blocked)
        {
            throw new BankingApplicationException("Cannot activate a blocked card. Please contact support.");
        }

        if (card.ExpiryDate < DateTime.UtcNow)
        {
            throw new BankingApplicationException("Cannot activate an expired card");
        }

        card.Status = CardStatus.Active;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Card {CardId} activated by user {UserId}",
            card.Id,
            _currentUserService.UserId);

        return Unit.Value;
    }
}
