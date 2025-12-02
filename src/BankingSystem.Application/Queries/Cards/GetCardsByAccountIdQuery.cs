namespace BankingSystem.Application.Queries.Cards;

using BankingSystem.Application.DTOs.Cards;
using MediatR;

public class GetCardsByAccountIdQuery : IRequest<IEnumerable<CardDto>>
{
    public Guid AccountId { get; set; }
}
