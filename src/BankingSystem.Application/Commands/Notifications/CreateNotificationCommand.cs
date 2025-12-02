namespace BankingSystem.Application.Commands.Notifications;

using MediatR;

public class CreateNotificationCommand : IRequest<Guid>
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Type { get; set; }
}
