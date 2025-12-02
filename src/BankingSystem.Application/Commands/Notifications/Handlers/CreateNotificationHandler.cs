using MediatR;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Commands.Notifications;
using BankingSystem.Domain.Entities;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Constants;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Application.Commands.Notifications.Handlers;

public class CreateNotificationHandler : IRequestHandler<CreateNotificationCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateNotificationHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
            throw new NotFoundException(string.Format(ValidationMessages.UserNotFound, request.UserId));

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        return notification.Id;
    }
}
