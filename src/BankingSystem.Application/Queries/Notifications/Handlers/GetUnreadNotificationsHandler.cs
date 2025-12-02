using BankingSystem.Application.DTOs.Notifications;
using BankingSystem.Application.Interfaces;
using BankingSystem.Application.Queries.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Application.Queries.Notifications.Handlers;

public class GetUnreadNotificationsHandler : IRequestHandler<GetUnreadNotificationsQuery, IEnumerable<NotificationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;

    public GetUnreadNotificationsHandler(
        IApplicationDbContext context,
        ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<NotificationDto>> Handle(GetUnreadNotificationsQuery request, CancellationToken cancellationToken)
    {
        // 1. Try to get from cache first (short TTL - 1 minute for real-time feel)
        var cacheKey = $"notification:unread:user:{request.UserId}";
        var cachedNotifications = await _cacheService.GetAsync<List<NotificationDto>>(cacheKey, cancellationToken);
        
        if (cachedNotifications != null)
            return cachedNotifications;
        
        // 2. If not in cache, query database with optimized query
        var notifications = await _context.Notifications
            .AsNoTracking() // Read-only optimization
            .Where(n => n.UserId == request.UserId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt) // Use index
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt,
                IsRead = n.IsRead
            })
            .ToListAsync(cancellationToken);

        // 3. Store in cache for 1 minute (notifications change frequently)
        await _cacheService.SetAsync(cacheKey, notifications, TimeSpan.FromMinutes(1), cancellationToken);
        
        return notifications;
    }
}
