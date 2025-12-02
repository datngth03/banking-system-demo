using MediatR;

namespace BankingSystem.Application.Commands.Users;

/// <summary>
/// Command to unlock a user account (Admin only)
/// </summary>
public class UnlockAccountCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    public Guid AdminUserId { get; set; }
}
