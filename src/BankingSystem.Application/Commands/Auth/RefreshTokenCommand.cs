namespace BankingSystem.Application.Commands.Auth;

using BankingSystem.Application.DTOs.Auth;
using MediatR;

public class RefreshTokenCommand : IRequest<AuthResponseDto>
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
