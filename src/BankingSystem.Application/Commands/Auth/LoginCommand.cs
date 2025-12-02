namespace BankingSystem.Application.Commands.Auth;

using BankingSystem.Application.DTOs.Auth;
using MediatR;

public class LoginCommand : IRequest<AuthResponseDto>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
