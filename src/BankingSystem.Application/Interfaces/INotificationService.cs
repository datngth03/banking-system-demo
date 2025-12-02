namespace BankingSystem.Application.Interfaces;

public interface INotificationService
{
    Task CreateNotificationAsync(Guid userId, string title, string message, string? type = null, CancellationToken cancellationToken = default);
    Task SendNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<dynamic>> GetUnreadNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);
}
