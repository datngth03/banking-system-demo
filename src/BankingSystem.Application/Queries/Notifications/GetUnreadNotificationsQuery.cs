namespace BankingSystem.Application.Queries.Notifications;

using BankingSystem.Application.DTOs.Notifications;
using MediatR;

public class GetUnreadNotificationsQuery : IRequest<IEnumerable<NotificationDto>>
{
    public Guid UserId { get; set; }
}
