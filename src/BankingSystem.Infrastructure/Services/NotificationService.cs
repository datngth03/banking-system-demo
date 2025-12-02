namespace BankingSystem.Infrastructure.Services;

using BankingSystem.Application.Interfaces;
using BankingSystem.Domain.Entities;
using BankingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class NotificationService : INotificationService
{
    private readonly BankingSystemDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        BankingSystemDbContext context,
        ICacheService cacheService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task CreateNotificationAsync(
        Guid userId,
        string title,
        string message,
        string? type = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate user's unread notifications cache
        await _cacheService.RemoveAsync($"notification:unread:user:{userId}", cancellationToken);

        _logger.LogInformation("Notification created with ID {NotificationId} for user {UserId}", notification.Id, userId);
    }

    public async Task SendNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(
            n => n.Id == notificationId,
            cancellationToken);

        if (notification is null)
        {
            _logger.LogWarning("Notification with ID {NotificationId} not found", notificationId);
            return;
        }

        // TODO: Implement actual notification sending (push notifications, SMS, etc.)
        _logger.LogInformation("Sending notification {NotificationId} to user {UserId}", notificationId, notification.UserId);

        await Task.CompletedTask;
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(
            n => n.Id == notificationId,
            cancellationToken);

        if (notification is null)
        {
            _logger.LogWarning("Notification with ID {NotificationId} not found", notificationId);
            return;
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate user's unread notifications cache
        await _cacheService.RemoveAsync($"notification:unread:user:{notification.UserId}", cancellationToken);

        _logger.LogInformation("Notification {NotificationId} marked as read", notificationId);
    }

    public async Task<IEnumerable<dynamic>> GetUnreadNotificationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _context.Notifications
            .AsNoTracking() // Read-only optimization
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                n.Id,
                n.Title,
                n.Message,
                n.Type,
                n.CreatedAt,
                n.ReadAt
            })
            .ToListAsync(cancellationToken);

        return notifications;
    }
}
