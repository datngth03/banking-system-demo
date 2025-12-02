namespace BankingSystem.Application.Queries.Users;

using MediatR;
using BankingSystem.Application.DTOs.Users;

public class GetUserByIdQuery : IRequest<UserDto?>
{
    public Guid Id { get; set; }
}
