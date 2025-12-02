namespace BankingSystem.Application.Commands.Users;

using MediatR;

public class DeleteUserCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
}
