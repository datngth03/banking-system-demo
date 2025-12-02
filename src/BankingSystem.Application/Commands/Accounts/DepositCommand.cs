namespace BankingSystem.Application.Commands.Accounts;

using MediatR;

/// <summary>
/// Command to deposit money into an account
/// </summary>
public class DepositCommand : IRequest<Unit>
{
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? ReferenceNumber { get; set; }
}
