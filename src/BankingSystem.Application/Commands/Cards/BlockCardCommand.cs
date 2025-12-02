namespace BankingSystem.Application.Commands.Cards;

using MediatR;

public class BlockCardCommand : IRequest<Unit>
{
    public Guid CardId { get; set; }
    public string? Reason { get; set; }
}
