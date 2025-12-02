namespace BankingSystem.Application.Commands.Accounts;

using MediatR;

/// <summary>
/// Command to withdraw money from an account
/// </summary>
public class WithdrawCommand : IRequest<Unit>
{
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}
