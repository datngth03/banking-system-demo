namespace BankingSystem.Application.Commands.Cards;

using MediatR;

/// <summary>
/// Command to activate a card
/// </summary>
public class ActivateCardCommand : IRequest<Unit>
{
    public Guid CardId { get; set; }
    public string LastFourDigits { get; set; } = string.Empty; // For verification
}
