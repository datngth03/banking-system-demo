namespace BankingSystem.Application.Commands.Cards;

using MediatR;

/// <summary>
/// Command to issue a new card for an account
/// </summary>
public class IssueCardCommand : IRequest<Guid>
{
    public Guid AccountId { get; set; }
    public string CardType { get; set; } = string.Empty; // Debit or Credit
    public string? NameOnCard { get; set; }
}
