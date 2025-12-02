namespace BankingSystem.Application.Queries.Cards;

using BankingSystem.Application.DTOs.Cards;
using MediatR;

public class GetCardsByUserIdQuery : IRequest<IEnumerable<CardDto>>
{
    public Guid UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
